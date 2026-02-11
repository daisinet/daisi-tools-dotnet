using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Daisi.Tools.Tests.Information
{
    public class WebSearchToolTests
    {
        private static MockToolContext CreateContextWithMockHttp(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory>(new MockHttpClientFactory(handler));
            var provider = services.BuildServiceProvider();
            return new MockToolContext(services: provider);
        }

        #region Tool Configuration Tests

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
        public void Name_ReturnsExpectedValue()
        {
            var tool = new WebSearchTool();
            Assert.Equal("Daisi Web Search", tool.Name);
        }

        [Fact]
        public void UseInstructions_MentionsJsonArrayOfUrls()
        {
            var tool = new WebSearchTool();
            Assert.Contains("JSON array", tool.UseInstructions);
            Assert.Contains("URL", tool.UseInstructions);
        }

        #endregion

        #region ExtractUrls Tests

        [Fact]
        public void ExtractUrls_GoogleStyleUrlParam_ParsesCorrectly()
        {
            var html = @"<a href=""/url?url=https://example.com/page1&amp;other=stuff"">Link1</a>
                         <a href=""/url?url=https://example.org/page2&amp;other=stuff"">Link2</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Equal(2, urls.Length);
            Assert.Contains("https://example.com/page1", urls);
            Assert.Contains("https://example.org/page2", urls);
        }

        [Fact]
        public void ExtractUrls_GoogleStyleQParam_ParsesCorrectly()
        {
            var html = @"<a href=""/url?q=https://example.com/result1&amp;sa=U&amp;ved=abc"">Link1</a>
                         <a href=""/url?q=https://example.org/result2&amp;sa=U&amp;ved=def"">Link2</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Equal(2, urls.Length);
            Assert.Contains("https://example.com/result1", urls);
            Assert.Contains("https://example.org/result2", urls);
        }

        [Fact]
        public void ExtractUrls_ExcludesGoogleAndYoutubeUrls()
        {
            var html = @"<a href=""/url?q=https://www.google.com/something&amp;foo=bar"">Google</a>
                         <a href=""/url?q=https://youtube.com/watch&amp;foo=bar"">YouTube</a>
                         <a href=""/url?q=https://www.youtube.com/video&amp;foo=bar"">YouTube2</a>
                         <a href=""/url?q=https://example.com/real&amp;foo=bar"">Real</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Single(urls);
            Assert.Contains("https://example.com/real", urls);
        }

        [Fact]
        public void ExtractUrls_RespectsMaxResults()
        {
            var html = @"<a href=""/url?q=https://a.com/1&amp;x=1"">1</a>
                         <a href=""/url?q=https://b.com/2&amp;x=1"">2</a>
                         <a href=""/url?q=https://c.com/3&amp;x=1"">3</a>";

            var urls = WebSearchTool.ExtractUrls(html, 2);

            Assert.Equal(2, urls.Length);
        }

        [Fact]
        public void ExtractUrls_DeduplicatesUrls()
        {
            var html = @"<a href=""/url?q=https://example.com/page&amp;x=1"">Link1</a>
                         <a href=""/url?q=https://example.com/page&amp;x=2"">Link2</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Single(urls);
        }

        [Fact]
        public void ExtractUrls_EmptyHtml_ReturnsEmpty()
        {
            var urls = WebSearchTool.ExtractUrls("", 5);
            Assert.Empty(urls);
        }

        [Fact]
        public void ExtractUrls_DecodesPercentEncodedUrls()
        {
            var html = @"<a href=""/url?q=https%3A%2F%2Fexample.com%2Fpath%3Fkey%3Dvalue&amp;sa=U"">Link</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Single(urls);
            Assert.Equal("https://example.com/path?key=value", urls[0]);
        }

        [Fact]
        public void ExtractUrls_HttpAndHttps_BothWork()
        {
            var html = @"<a href=""url=https://secure.example.com/page&amp;x=1"">HTTPS</a>
                         <a href=""url=http://plain.example.com/page&amp;x=1"">HTTP</a>";

            var urls = WebSearchTool.ExtractUrls(html, 10);

            Assert.Equal(2, urls.Length);
            Assert.Contains("https://secure.example.com/page", urls);
            Assert.Contains("http://plain.example.com/page", urls);
        }

        #endregion

        #region Tool Execution Tests

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
        public async Task Execute_ReturnsJsonArrayOfUrlStrings()
        {
            var googleHtml = CreateGoogleHtml(
                "https://example.com/result1",
                "https://example.org/result2",
                "https://test.com/result3"
            );

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var context = CreateContextWithMockHttp(handler);

            var tool = new WebSearchTool();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test query", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);

            var parsed = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(parsed);
            Assert.Equal(3, parsed!.Length);
            Assert.Equal("https://example.com/result1", parsed[0]);
            Assert.Equal("https://example.org/result2", parsed[1]);
            Assert.Equal("https://test.com/result3", parsed[2]);
        }

        [Fact]
        public async Task Execute_SendsUserAgentHeader()
        {
            var googleHtml = CreateGoogleHtml("https://example.com/result");

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var context = CreateContextWithMockHttp(handler);

            var tool = new WebSearchTool();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.NotNull(handler.LastRequest);
            var userAgentValues = handler.LastRequest!.Headers.UserAgent.ToString();
            Assert.Contains("Mozilla/5.0", userAgentValues);
            Assert.Contains("AppleWebKit", userAgentValues);
        }

        [Fact]
        public async Task Execute_SendsRequestToGoogleSearchUrl()
        {
            var googleHtml = CreateGoogleHtml("https://example.com/result");

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var context = CreateContextWithMockHttp(handler);

            var tool = new WebSearchTool();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "my search", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.NotNull(handler.LastRequest);
            var uri = handler.LastRequest!.RequestUri!;
            Assert.StartsWith(WebSearchTool.GoogleSearchBaseUrl, uri.ToString());
            // Uri.EscapeDataString encodes the query; verify it's passed correctly
            Assert.Contains("q=", uri.Query);
            Assert.Contains("search", uri.Query);
        }

        [Fact]
        public async Task Execute_HttpError_ReturnsFailure()
        {
            var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
            var context = CreateContextWithMockHttp(handler);

            var tool = new WebSearchTool();

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
        public void ExecutionContext_HasCorrectMessage()
        {
            var tool = new WebSearchTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "how to cook pasta", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);

            Assert.Equal("Searching the web for: how to cook pasta", execContext.ExecutionMessage);
        }

        [Fact]
        public async Task Execute_RealisticGoogleHtml_ExtractsUrls()
        {
            var html = @"
<!DOCTYPE html>
<html>
<head><title>test - Google Search</title></head>
<body>
<div id=""search"">
  <div class=""g"">
    <a href=""/url?q=https://en.wikipedia.org/wiki/Test&amp;sa=U&amp;ved=2ahUKE"">
      <h3>Test - Wikipedia</h3>
    </a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://www.merriam-webster.com/dictionary/test&amp;sa=U&amp;ved=3bhVLE"">
      <h3>Test Definition</h3>
    </a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://www.google.com/settings&amp;sa=U"">Settings</a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://example.org/testing&amp;sa=U&amp;ved=4ciWMF"">
      <h3>Testing Resources</h3>
    </a>
  </div>
</div>
</body>
</html>";

            var handler = new MockHttpMessageHandler(html, HttpStatusCode.OK);
            var context = CreateContextWithMockHttp(handler);

            var tool = new WebSearchTool();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(urls);
            // Should have Wikipedia, Merriam-Webster, and example.org but NOT google.com
            Assert.Equal(3, urls!.Length);
            Assert.Contains("https://en.wikipedia.org/wiki/Test", urls);
            Assert.Contains("https://www.merriam-webster.com/dictionary/test", urls);
            Assert.Contains("https://example.org/testing", urls);
            Assert.DoesNotContain(urls, u => u.Contains("google.com"));
        }

        #endregion

        #region Helpers

        private static string CreateGoogleHtml(params string[] urls)
        {
            var links = string.Join("\n", urls.Select(u =>
                $@"<a href=""/url?q={u}&amp;sa=U&amp;ved=abc"">Result</a>"));

            return $@"
<!DOCTYPE html>
<html><body>
<div id=""search"">
{links}
</div>
</body></html>";
        }

        #endregion
    }

    /// <summary>
    /// Mock HttpMessageHandler for testing HTTP requests without hitting real APIs.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_responseContent)
            });
        }
    }

    /// <summary>
    /// HttpMessageHandler that throws an exception for testing error handling.
    /// </summary>
    public class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    /// <summary>
    /// Mock IHttpClientFactory that creates HttpClient instances using a provided handler.
    /// </summary>
    public class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public MockHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }
    }
}
