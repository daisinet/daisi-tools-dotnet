using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Social.X.Tools;

namespace Daisi.SecureTools.Tests.Social;

public class XPostToolTests
{
    private static SocialHttpClient CreateSocialClient(SocialMockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task XPostTool_PostsTextTweet()
    {
        var response = JsonSerializer.Serialize(new
        {
            data = new { id = "tweet-123", text = "Hello world" }
        });

        var handler = new SocialMockHttpHandler(response);
        var socialClient = CreateSocialClient(handler);
        var tool = new XPostTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
            [new ParameterValue { Name = "text", Value = "Hello world" }]);

        Assert.True(result.Success);
        Assert.Contains("tweet-123", result.Output);
        Assert.Equal("json", result.OutputFormat);
        Assert.Contains("/2/tweets", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task XPostTool_RequiresText()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new XPostTool(new MockHttpClientFactory(handler));
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token", []);

        Assert.False(result.Success);
        Assert.Contains("text", result.ErrorMessage);
    }

    [Fact]
    public async Task XPostTool_IncludesReplyToId()
    {
        var response = JsonSerializer.Serialize(new
        {
            data = new { id = "reply-456" }
        });

        var handler = new SocialMockHttpHandler(response);
        var socialClient = CreateSocialClient(handler);
        var tool = new XPostTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "text", Value = "Replying!" },
            new ParameterValue { Name = "replyToId", Value = "original-789" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("\"isReply\":true", result.Output);
    }

    [Fact]
    public async Task XPostTool_IncludesQuoteTweetId()
    {
        var response = JsonSerializer.Serialize(new
        {
            data = new { id = "quote-101" }
        });

        var handler = new SocialMockHttpHandler(response);
        var socialClient = CreateSocialClient(handler);
        var tool = new XPostTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "text", Value = "Quoting this!" },
            new ParameterValue { Name = "quoteTweetId", Value = "qt-202" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("\"isQuote\":true", result.Output);
    }
}
