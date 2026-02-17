using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Firecrawl;
using Daisi.SecureTools.Firecrawl.Tools;

namespace Daisi.SecureTools.Tests.Firecrawl;

public class FirecrawlToolTests
{
    private static FirecrawlClient CreateClient(MockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new FirecrawlClient(factory);
    }

    // --- ScrapeTool ---

    [Fact]
    public async Task ScrapeTool_ReturnsScrapedContent()
    {
        var response = JsonSerializer.Serialize(new
        {
            success = true,
            data = new { markdown = "# Hello World\nThis is scraped content." }
        });

        var handler = new MockHttpHandler(response);
        var tool = new ScrapeTool(CreateClient(handler));
        var result = await tool.ExecuteAsync("test-key", "https://api.firecrawl.dev",
            [new ParameterValue { Name = "url", Value = "https://example.com" }]);

        Assert.True(result.Success);
        Assert.Contains("Hello World", result.Output);
        Assert.Equal("markdown", result.OutputFormat);
        Assert.Contains("/v1/scrape", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("Bearer test-key", handler.LastRequest.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task ScrapeTool_RequiresUrl()
    {
        var tool = new ScrapeTool(CreateClient(new MockHttpHandler("{}")));
        var result = await tool.ExecuteAsync("key", "https://api.firecrawl.dev", []);

        Assert.False(result.Success);
        Assert.Contains("url", result.ErrorMessage);
    }

    // --- CrawlTool ---

    [Fact]
    public async Task CrawlTool_StartsAsyncCrawl()
    {
        var response = JsonSerializer.Serialize(new
        {
            success = true,
            id = "crawl-job-123",
            url = "https://api.firecrawl.dev/v1/crawl/crawl-job-123"
        });

        var handler = new MockHttpHandler(response);
        var tool = new CrawlTool(CreateClient(handler));
        var result = await tool.ExecuteAsync("test-key", "https://api.firecrawl.dev",
        [
            new ParameterValue { Name = "url", Value = "https://example.com" },
            new ParameterValue { Name = "maxPages", Value = "5" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("crawl-job-123", result.Output);
        Assert.Contains("/v1/crawl", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("\"limit\":5", handler.LastRequestBody!);
    }

    [Fact]
    public async Task CrawlTool_RequiresUrl()
    {
        var tool = new CrawlTool(CreateClient(new MockHttpHandler("{}")));
        var result = await tool.ExecuteAsync("key", "https://api.firecrawl.dev", []);

        Assert.False(result.Success);
        Assert.Contains("url", result.ErrorMessage);
    }

    // --- SearchTool ---

    [Fact]
    public async Task SearchTool_ReturnsSearchResults()
    {
        var response = JsonSerializer.Serialize(new
        {
            success = true,
            data = new[]
            {
                new { url = "https://example.com", title = "Example", markdown = "Content" }
            }
        });

        var handler = new MockHttpHandler(response);
        var tool = new SearchTool(CreateClient(handler));
        var result = await tool.ExecuteAsync("test-key", "https://api.firecrawl.dev",
            [new ParameterValue { Name = "query", Value = "test query" }]);

        Assert.True(result.Success);
        Assert.Contains("example.com", result.Output);
        Assert.Contains("/v1/search", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchTool_RequiresQuery()
    {
        var tool = new SearchTool(CreateClient(new MockHttpHandler("{}")));
        var result = await tool.ExecuteAsync("key", "https://api.firecrawl.dev", []);

        Assert.False(result.Success);
        Assert.Contains("query", result.ErrorMessage);
    }

    // --- ExtractTool ---

    [Fact]
    public async Task ExtractTool_ReturnsExtractedData()
    {
        var response = JsonSerializer.Serialize(new
        {
            success = true,
            data = new { name = "John Doe", email = "john@example.com" }
        });

        var handler = new MockHttpHandler(response);
        var tool = new ExtractTool(CreateClient(handler));
        var result = await tool.ExecuteAsync("test-key", "https://api.firecrawl.dev",
        [
            new ParameterValue { Name = "url", Value = "https://example.com/contact" },
            new ParameterValue { Name = "prompt", Value = "Extract contact information" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("John Doe", result.Output);
        Assert.Contains("/v1/extract", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ExtractTool_RequiresUrlAndPrompt()
    {
        var tool = new ExtractTool(CreateClient(new MockHttpHandler("{}")));

        var result1 = await tool.ExecuteAsync("key", "https://api.firecrawl.dev", []);
        Assert.False(result1.Success);

        var result2 = await tool.ExecuteAsync("key", "https://api.firecrawl.dev",
            [new ParameterValue { Name = "url", Value = "https://example.com" }]);
        Assert.False(result2.Success);
    }

    // --- MapTool ---

    [Fact]
    public async Task MapTool_ReturnsSiteMap()
    {
        var response = JsonSerializer.Serialize(new
        {
            success = true,
            links = new[] { "https://example.com/", "https://example.com/about", "https://example.com/contact" }
        });

        var handler = new MockHttpHandler(response);
        var tool = new MapTool(CreateClient(handler));
        var result = await tool.ExecuteAsync("test-key", "https://api.firecrawl.dev",
            [new ParameterValue { Name = "url", Value = "https://example.com" }]);

        Assert.True(result.Success);
        Assert.Contains("about", result.Output);
        Assert.Contains("/v1/map", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task MapTool_RequiresUrl()
    {
        var tool = new MapTool(CreateClient(new MockHttpHandler("{}")));
        var result = await tool.ExecuteAsync("key", "https://api.firecrawl.dev", []);

        Assert.False(result.Success);
        Assert.Contains("url", result.ErrorMessage);
    }

    // --- FirecrawlClient ---

    [Fact]
    public async Task FirecrawlClient_ThrowsOnApiError()
    {
        var handler = new MockHttpHandler("{\"error\": \"rate limited\"}", System.Net.HttpStatusCode.TooManyRequests);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<FirecrawlException>(() =>
            client.PostAsync("key", "https://api.firecrawl.dev", "/v1/scrape", new { url = "test" }));
    }

    [Fact]
    public async Task FirecrawlClient_SetsAuthHeader()
    {
        var handler = new MockHttpHandler("{}");
        var client = CreateClient(handler);

        await client.GetAsync("my-api-key", "https://api.firecrawl.dev", "/v1/test");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("my-api-key", handler.LastRequest.Headers.Authorization.Parameter);
    }

    private class MockHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }
}
