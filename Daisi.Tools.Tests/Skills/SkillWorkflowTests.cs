using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Integration;
using Daisi.Tools.Tests.Helpers;
using Daisi.Tools.Tests.Information;
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
        public async Task Research_SearchThenFetchThenSummarize()
        {
            // Simulate: WebSearch → HttpGet → HtmlToMarkdown → SummarizeText

            // Step 1: WebSearch returns URLs
            var googleHtml = ToolTestHelpers.CreateMockGoogleHtml("https://example.com/article1");
            var searchHandler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var searchContext = ToolTestHelpers.BuildContextWithMockHttp(searchHandler);

            var searchTool = new WebSearchTool();
            var searchParams = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "artificial intelligence research", IsRequired = true }
            };
            var searchExec = searchTool.GetExecutionContext(searchContext, CancellationToken.None, searchParams);
            var searchResult = await searchExec.ExecutionTask;
            Assert.True(searchResult.Success);

            // Step 2: HttpGet fetches the first URL
            var pageHtml = "<html><body><h1>AI Research</h1><p>Latest findings in AI.</p></body></html>";
            var fetchHandler = new MockHttpMessageHandler(pageHtml, HttpStatusCode.OK);
            var fetchContext = ToolTestHelpers.BuildContextWithMockHttp(fetchHandler);

            var httpGet = new HttpGetTool();
            var fetchParams = new ToolParameterBase[]
            {
                new() { Name = "url", Value = "https://example.com/article1", IsRequired = true }
            };
            var fetchExec = httpGet.GetExecutionContext(fetchContext, CancellationToken.None, fetchParams);
            var fetchResult = await fetchExec.ExecutionTask;
            Assert.True(fetchResult.Success);

            // Step 3: Convert to Markdown
            var convertResult = HtmlToMarkdownTool.Execute(fetchResult.Output);
            Assert.True(convertResult.Success);

            // Step 4: Summarize
            var summarize = new SummarizeTextTool();
            var summaryContext = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "AI research is advancing rapidly." }));
            var summaryParams = new ToolParameterBase[]
            {
                new() { Name = "text", Value = convertResult.Output, IsRequired = true }
            };
            var summaryExec = summarize.GetExecutionContext(summaryContext, CancellationToken.None, summaryParams);
            var summaryResult = await summaryExec.ExecutionTask;
            Assert.True(summaryResult.Success);
        }

        [Fact]
        public async Task DateAwareSearch_GetDateThenSearch()
        {
            // Simulate: DateTime → WebSearch

            // Step 1: Get current date
            var dateResult = DateTimeTool.Execute("now", null, null, "yyyy", "UTC");
            Assert.True(dateResult.Success);
            var currentYear = dateResult.Output;

            // Step 2: Use date in search query
            var googleHtml = ToolTestHelpers.CreateMockGoogleHtml("https://news.example.com/ai-2025");
            var searchHandler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var searchContext = ToolTestHelpers.BuildContextWithMockHttp(searchHandler);

            var searchTool = new WebSearchTool();
            var searchParams = new ToolParameterBase[]
            {
                new() { Name = "query", Value = $"AI regulation news {currentYear}", IsRequired = true }
            };
            var searchExec = searchTool.GetExecutionContext(searchContext, CancellationToken.None, searchParams);
            var searchResult = await searchExec.ExecutionTask;
            Assert.True(searchResult.Success);
        }

        [Fact]
        public async Task FactCheck_SearchAndWikipedia()
        {
            // Simulate: WebSearch + WikipediaSearch in parallel

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

            // Web search
            var googleHtml = ToolTestHelpers.CreateMockGoogleHtml("https://physics.example.com/speed-of-light");
            var searchHandler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var searchContext = ToolTestHelpers.BuildContextWithMockHttp(searchHandler);

            var searchTool = new WebSearchTool();
            var searchParams = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "speed of light fact check", IsRequired = true }
            };
            var searchExec = searchTool.GetExecutionContext(searchContext, CancellationToken.None, searchParams);

            // Execute both in parallel
            var wikiTask = wikiExec.ExecutionTask;
            var searchTask = searchExec.ExecutionTask;

            await Task.WhenAll(wikiTask, searchTask);

            Assert.True(wikiTask.Result.Success);
            Assert.True(searchTask.Result.Success);
        }
    }
}
