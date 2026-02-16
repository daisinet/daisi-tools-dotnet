using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Integration;
using Daisi.Tools.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Daisi.Tools.Tests.Integration
{
    public class GrokipediaSearchToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new GrokipediaSearchTool();
            Assert.Equal("daisi-integration-grokipedia", tool.Id);
        }

        [Fact]
        public void Parameters_QueryIsRequired()
        {
            var tool = new GrokipediaSearchTool();
            Assert.True(tool.Parameters.First(p => p.Name == "query").IsRequired);
        }

        [Fact]
        public void Parameters_MaxResultsIsOptional()
        {
            var tool = new GrokipediaSearchTool();
            Assert.False(tool.Parameters.First(p => p.Name == "max-results").IsRequired);
        }

        [Fact]
        public void ParseResults_ExtractsTitleAndSnippet()
        {
            var json = ToolTestHelpers.CreateMockGrokipediaResponse(
                ("Albert Einstein", "German-born theoretical physicist"),
                ("Theory of Relativity", "Two interrelated physics theories"));

            var results = GrokipediaSearchTool.ParseResults(json);

            Assert.Equal(2, results.Length);
            Assert.Equal("Albert Einstein", results[0].Title);
            Assert.Contains("theoretical physicist", results[0].Snippet);
            Assert.Contains("grokipedia.com", results[0].Url);
        }

        [Fact]
        public void ParseResults_StripsHtmlFromSnippets()
        {
            var json = @"{""results"":[{""title"":""Test"",""snippet"":""<span class='highlight'>Bold</span> text"",""slug"":""Test"",""relevanceScore"":100.0,""viewCount"":""0""}],""totalCount"":1}";
            var results = GrokipediaSearchTool.ParseResults(json);

            Assert.DoesNotContain("<span", results[0].Snippet);
            Assert.Contains("Bold", results[0].Snippet);
        }

        [Fact]
        public async Task Execute_MissingHttpClientFactory_ReturnsError()
        {
            var tool = new GrokipediaSearchTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
            Assert.Contains("HttpClientFactory", result.ErrorMessage);
        }

        [Fact]
        public async Task Execute_WithMockHttp_ReturnsResults()
        {
            var grokipediaResponse = ToolTestHelpers.CreateMockGrokipediaResponse(
                ("Test Article", "This is a test article about testing"));

            var handler = new MockHttpMessageHandler(grokipediaResponse, HttpStatusCode.OK);
            var context = ToolTestHelpers.BuildContextWithMockHttp(handler);

            var tool = new GrokipediaSearchTool();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);
        }

        [Fact]
        public async Task Execute_HttpError_ReturnsFailure()
        {
            var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
            var context = ToolTestHelpers.BuildContextWithMockHttp(handler);

            var tool = new GrokipediaSearchTool();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
            Assert.Contains("Connection refused", result.ErrorMessage);
        }

        [Fact]
        public async Task Execute_UrlContainsQuery()
        {
            var grokipediaResponse = ToolTestHelpers.CreateMockGrokipediaResponse(("Result", "Snippet"));
            var handler = new MockHttpMessageHandler(grokipediaResponse, HttpStatusCode.OK);
            var context = ToolTestHelpers.BuildContextWithMockHttp(handler);

            var tool = new GrokipediaSearchTool();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "quantum physics", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.NotNull(handler.LastRequest);
            Assert.Contains("query=quantum", handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task LiveSearch_ReturnsResultsFromGrokipedia()
        {
            // Integration test: hits the real Grokipedia API
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty);
            var provider = services.BuildServiceProvider();
            var context = new MockToolContext(services: provider);

            var tool = new GrokipediaSearchTool();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "Albert Einstein", IsRequired = true },
                new() { Name = "max-results", Value = "3", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);
            Assert.Contains("Einstein", result.Output);
            Assert.Contains("grokipedia.com", result.Output);
            Assert.Contains("Found", result.OutputMessage);
        }
    }
}
