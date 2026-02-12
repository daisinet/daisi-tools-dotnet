using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Integration;
using Daisi.Tools.Tests.Helpers;
using System.Net;
using System.Text.Json;

namespace Daisi.Tools.Tests.Integration
{
    public class WikipediaSearchToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new WikipediaSearchTool();
            Assert.Equal("daisi-integration-wikipedia", tool.Id);
        }

        [Fact]
        public void Parameters_QueryIsRequired()
        {
            var tool = new WikipediaSearchTool();
            Assert.True(tool.Parameters.First(p => p.Name == "query").IsRequired);
        }

        [Fact]
        public void Parameters_MaxResultsIsOptional()
        {
            var tool = new WikipediaSearchTool();
            Assert.False(tool.Parameters.First(p => p.Name == "max-results").IsRequired);
        }

        [Fact]
        public void ParseResults_ExtractsTitleAndSnippet()
        {
            var json = ToolTestHelpers.CreateMockWikipediaResponse(
                ("Albert Einstein", "German-born theoretical physicist"),
                ("Theory of Relativity", "Two interrelated physics theories"));

            var results = WikipediaSearchTool.ParseResults(json);

            Assert.Equal(2, results.Length);
            Assert.Equal("Albert Einstein", results[0].Title);
            Assert.Contains("theoretical physicist", results[0].Snippet);
            Assert.Contains("wikipedia.org", results[0].Url);
        }

        [Fact]
        public void ParseResults_StripsHtmlFromSnippets()
        {
            var json = @"{""query"":{""search"":[{""title"":""Test"",""snippet"":""<span class='highlight'>Bold</span> text"",""pageid"":1}]}}";
            var results = WikipediaSearchTool.ParseResults(json);

            Assert.DoesNotContain("<span", results[0].Snippet);
            Assert.Contains("Bold", results[0].Snippet);
        }

        [Fact]
        public async Task Execute_MissingHttpClientFactory_ReturnsError()
        {
            var tool = new WikipediaSearchTool();
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
            var wikiResponse = ToolTestHelpers.CreateMockWikipediaResponse(
                ("Test Article", "This is a test article about testing"));

            var handler = new MockHttpMessageHandler(wikiResponse, HttpStatusCode.OK);
            var context = ToolTestHelpers.BuildContextWithMockHttp(handler);

            var tool = new WikipediaSearchTool();
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

            var tool = new WikipediaSearchTool();
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
            var wikiResponse = ToolTestHelpers.CreateMockWikipediaResponse(("Result", "Snippet"));
            var handler = new MockHttpMessageHandler(wikiResponse, HttpStatusCode.OK);
            var context = ToolTestHelpers.BuildContextWithMockHttp(handler);

            var tool = new WikipediaSearchTool();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "quantum physics", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.NotNull(handler.LastRequest);
            Assert.Contains("srsearch=quantum", handler.LastRequest!.RequestUri!.ToString());
        }
    }
}
