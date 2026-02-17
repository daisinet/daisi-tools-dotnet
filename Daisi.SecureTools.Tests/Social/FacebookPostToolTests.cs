using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Social.Facebook.Tools;

namespace Daisi.SecureTools.Tests.Social;

public class FacebookPostToolTests
{
    private static SocialHttpClient CreateSocialClient(SocialMockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task FacebookPostTool_PostsTextToPage()
    {
        // First call: get page access token; Second call: post to feed
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new { access_token = "page-token-123" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "post-456" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new FacebookPostTool();

        var result = await tool.ExecuteAsync(socialClient, "user-token",
        [
            new ParameterValue { Name = "pageId", Value = "page-001" },
            new ParameterValue { Name = "message", Value = "Hello Facebook!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("post-456", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task FacebookPostTool_RequiresPageId()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new FacebookPostTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token", []);

        Assert.False(result.Success);
        Assert.Contains("pageId", result.ErrorMessage);
    }

    [Fact]
    public async Task FacebookPostTool_RequiresContent()
    {
        // Get page token succeeds, but no content to post
        var handler = new SocialMockHttpHandler(
            JsonSerializer.Serialize(new { access_token = "page-token" }));

        var tool = new FacebookPostTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "pageId", Value = "page-001" }]);

        Assert.False(result.Success);
        Assert.Contains("message", result.ErrorMessage);
    }

    [Fact]
    public async Task FacebookPostTool_PostsPhotoToPage()
    {
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new { access_token = "page-token" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "photo-789" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new FacebookPostTool();

        var result = await tool.ExecuteAsync(socialClient, "user-token",
        [
            new ParameterValue { Name = "pageId", Value = "page-001" },
            new ParameterValue { Name = "mediaUrl", Value = "https://example.com/image.jpg" },
            new ParameterValue { Name = "message", Value = "Check this out!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("photo", result.Output);
    }

    [Fact]
    public async Task FacebookPostTool_PostsLinkToPage()
    {
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new { access_token = "page-token" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "link-post-101" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new FacebookPostTool();

        var result = await tool.ExecuteAsync(socialClient, "user-token",
        [
            new ParameterValue { Name = "pageId", Value = "page-001" },
            new ParameterValue { Name = "link", Value = "https://example.com" },
            new ParameterValue { Name = "message", Value = "Interesting article" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("link-post-101", result.Output);
    }
}
