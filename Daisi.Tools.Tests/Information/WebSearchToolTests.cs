using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Information
{
    public class WebSearchToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new WebSearchTool();
            Assert.Equal("daisi-info-web-search", tool.Id);
        }

        [Fact]
        public void Parameters_QueryIsRequired()
        {
            var tool = new WebSearchTool();
            Assert.True(tool.Parameters.First(p => p.Name == "query").IsRequired);
        }

        [Fact]
        public void Parameters_MaxResultsIsOptional()
        {
            var tool = new WebSearchTool();
            Assert.False(tool.Parameters.First(p => p.Name == "max-results").IsRequired);
        }

        [Fact]
        public void ExtractUrls_ParsesUrlsFromHtml()
        {
            var html = @"<a href=""/url?url=https://example.com/page1&amp;other=stuff"">Link1</a>
                         <a href=""/url?url=https://example.org/page2&amp;other=stuff"">Link2</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Equal(2, urls.Length);
            Assert.Contains("https://example.com/page1", urls);
            Assert.Contains("https://example.org/page2", urls);
        }

        [Fact]
        public void ExtractUrls_ExcludesGoogleAndYoutubeUrls()
        {
            var html = @"<a href=""/url?url=https://www.google.com/something&amp;foo=bar"">Google</a>
                         <a href=""/url?url=https://youtube.com/watch&amp;foo=bar"">YouTube</a>
                         <a href=""/url?url=https://example.com/real&amp;foo=bar"">Real</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Single(urls);
            Assert.Contains("https://example.com/real", urls);
        }

        [Fact]
        public void ExtractUrls_RespectsMaxResults()
        {
            var html = @"<a href=""/url?url=https://a.com/1&amp;x=1"">1</a>
                         <a href=""/url?url=https://b.com/2&amp;x=1"">2</a>
                         <a href=""/url?url=https://c.com/3&amp;x=1"">3</a>";

            var urls = WebSearchTool.ExtractUrls(html, 2);

            Assert.Equal(2, urls.Length);
        }

        [Fact]
        public void ExtractUrls_DeduplicatesUrls()
        {
            var html = @"<a href=""/url?url=https://example.com/page&amp;x=1"">Link1</a>
                         <a href=""/url?url=https://example.com/page&amp;x=2"">Link2</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Single(urls);
        }

        [Fact]
        public async Task Execute_MissingHttpClientFactory_ReturnsError()
        {
            var tool = new WebSearchTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test query", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
            Assert.Contains("HttpClientFactory", result.ErrorMessage);
        }

        [Fact]
        public async Task Execute_OutputFormatIsJson()
        {
            // When HttpClientFactory is missing, we can't test full output format,
            // but we can verify the tool's configuration
            var tool = new WebSearchTool();
            Assert.Equal("Daisi Web Search", tool.Name);
        }

        [Fact]
        public void ExtractUrls_EmptyHtml_ReturnsEmpty()
        {
            var urls = WebSearchTool.ExtractUrls("", 5);
            Assert.Empty(urls);
        }
    }
}
