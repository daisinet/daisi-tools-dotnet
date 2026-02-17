using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Social.Reddit.Tools;

namespace Daisi.SecureTools.Tests.Social;

public class RedditSubmitToolTests
{
    private static SocialHttpClient CreateSocialClient(SocialMockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task RedditSubmitTool_SubmitsTextPost()
    {
        var response = JsonSerializer.Serialize(new
        {
            json = new
            {
                data = new { name = "t3_abc123", id = "abc123", url = "https://www.reddit.com/r/test/comments/abc123" }
            }
        });

        var handler = new SocialMockHttpHandler(response);
        var socialClient = CreateSocialClient(handler);
        var tool = new RedditSubmitTool();

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "subreddit", Value = "test" },
            new ParameterValue { Name = "title", Value = "Test Post" },
            new ParameterValue { Name = "text", Value = "This is a test post body." }
        ]);

        Assert.True(result.Success);
        Assert.Contains("t3_abc123", result.Output);
        Assert.Contains("\"kind\":\"self\"", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task RedditSubmitTool_SubmitsLinkPost()
    {
        var response = JsonSerializer.Serialize(new
        {
            json = new
            {
                data = new { name = "t3_def456", url = "https://www.reddit.com/r/test/comments/def456" }
            }
        });

        var handler = new SocialMockHttpHandler(response);
        var socialClient = CreateSocialClient(handler);
        var tool = new RedditSubmitTool();

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "subreddit", Value = "test" },
            new ParameterValue { Name = "title", Value = "Check this link" },
            new ParameterValue { Name = "url", Value = "https://example.com" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("\"kind\":\"link\"", result.Output);
    }

    [Fact]
    public async Task RedditSubmitTool_RequiresSubreddit()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new RedditSubmitTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "title", Value = "Test" }]);

        Assert.False(result.Success);
        Assert.Contains("subreddit", result.ErrorMessage);
    }

    [Fact]
    public async Task RedditSubmitTool_RequiresTitle()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new RedditSubmitTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "subreddit", Value = "test" }]);

        Assert.False(result.Success);
        Assert.Contains("title", result.ErrorMessage);
    }

    [Fact]
    public async Task RedditSubmitTool_SetsUserAgentHeader()
    {
        var response = JsonSerializer.Serialize(new
        {
            json = new { data = new { name = "t3_test" } }
        });

        var handler = new SocialMockHttpHandler(response);
        var socialClient = CreateSocialClient(handler);
        var tool = new RedditSubmitTool();

        await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "subreddit", Value = "test" },
            new ParameterValue { Name = "title", Value = "Test" }
        ]);

        Assert.Contains("daisi-securetools",
            handler.LastRequest!.Headers.UserAgent.ToString());
    }
}
