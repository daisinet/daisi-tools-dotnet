using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Integration;
using Daisi.Tools.Tests.Helpers;
using Daisi.Tools.Web.Clients;
using Daisi.Tools.Web.Html;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Daisi.Tools.Tests.Skills
{
    /// <summary>
    /// Tests multi-tool workflows that correspond to skill definitions.
    /// Each test executes tools in sequence, passing output from one to input of the next.
    /// </summary>
    public class SkillWorkflowTests
    {
        [Fact]
        public async Task WebsiteSummary_FetchThenConvertThenSummarize()
        {
            // Simulate: HttpGet → HtmlToMarkdown → SummarizeText

            // Step 1: HttpGet returns HTML
            var mockHtml = "<html><body><h1>Test Page</h1><p>This is important content about AI.</p></body></html>";
            var handler = new MockHttpMessageHandler(mockHtml, HttpStatusCode.OK);
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory>(new MockHttpClientFactory(handler));
            var provider = services.BuildServiceProvider();
            var httpContext = new MockToolContext(services: provider);

            var httpGetTool = new HttpGetTool();
            var httpParams = new ToolParameterBase[]
            {
                new() { Name = "url", Value = "https://example.com/article", IsRequired = true }
            };
            var httpExec = httpGetTool.GetExecutionContext(httpContext, CancellationToken.None, httpParams);
            var httpResult = await httpExec.ExecutionTask;
            Assert.True(httpResult.Success);

            // Step 2: HtmlToMarkdown converts the HTML
            var htmlToMd = new HtmlToMarkdownTool();
            var mdParams = new ToolParameterBase[]
            {
                new() { Name = "html", Value = httpResult.Output, IsRequired = true }
            };
            var mdExec = htmlToMd.GetExecutionContext(new MockToolContext(), CancellationToken.None, mdParams);
            var mdResult = await mdExec.ExecutionTask;
            Assert.True(mdResult.Success);
            Assert.Contains("# Test Page", mdResult.Output);

            // Step 3: SummarizeText summarizes the markdown
            var summarize = new SummarizeTextTool();
            var summaryContext = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "Summary: An article about AI." }));
            var summaryParams = new ToolParameterBase[]
            {
                new() { Name = "text", Value = mdResult.Output, IsRequired = true }
            };
            var summaryExec = summarize.GetExecutionContext(summaryContext, CancellationToken.None, summaryParams);
            var summaryResult = await summaryExec.ExecutionTask;
            Assert.True(summaryResult.Success);
            Assert.Contains("Summary", summaryResult.Output);
        }

        [Fact]
        public async Task FactCheck_WikipediaSearch()
        {
            // Wikipedia search
            var wikiResponse = ToolTestHelpers.CreateMockWikipediaResponse(
                ("Speed of light", "The speed of light in vacuum is 299,792,458 metres per second"));
            var wikiHandler = new MockHttpMessageHandler(wikiResponse, HttpStatusCode.OK);
            var wikiContext = ToolTestHelpers.BuildContextWithMockHttp(wikiHandler);

            var wikiTool = new WikipediaSearchTool();
            var wikiParams = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "speed of light", IsRequired = true }
            };
            var wikiExec = wikiTool.GetExecutionContext(wikiContext, CancellationToken.None, wikiParams);
            var wikiResult = await wikiExec.ExecutionTask;
            Assert.True(wikiResult.Success);
        }
    }
}
