using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Social.LinkedIn.Tools;

namespace Daisi.SecureTools.Tests.Social;

public class LinkedInPostToolTests
{
    private static SocialHttpClient CreateSocialClient(SocialMockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task LinkedInPostTool_PostsTextContent()
    {
        // First call: get user info; Second call: create post
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new { sub = "user123" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "urn:li:share:post-789" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new LinkedInPostTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
            [new ParameterValue { Name = "text", Value = "Hello LinkedIn!" }]);

        Assert.True(result.Success);
        Assert.Contains("urn:li:person:user123", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task LinkedInPostTool_RequiresText()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new LinkedInPostTool(new MockHttpClientFactory(handler));
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token", []);

        Assert.False(result.Success);
        Assert.Contains("text", result.ErrorMessage);
    }

    [Fact]
    public async Task LinkedInPostTool_SetsLinkedInVersionHeader()
    {
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new { sub = "user123" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "urn:li:share:post-1" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new LinkedInPostTool(new MockHttpClientFactory(handler));

        await tool.ExecuteAsync(socialClient, "test-token",
            [new ParameterValue { Name = "text", Value = "Testing headers" }]);

        // The second request (POST to /rest/posts) should have LinkedIn-Version header
        var postRequest = handler.Requests[1];
        Assert.True(postRequest.Headers.TryGetValues("LinkedIn-Version", out var values));
        Assert.Contains("202402", values);
    }

    [Fact]
    public async Task LinkedInPostTool_UsesCustomVisibility()
    {
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new { sub = "user123" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "post-1" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new LinkedInPostTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "text", Value = "Connections only" },
            new ParameterValue { Name = "visibility", Value = "CONNECTIONS" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("CONNECTIONS", result.Output);
    }
}
