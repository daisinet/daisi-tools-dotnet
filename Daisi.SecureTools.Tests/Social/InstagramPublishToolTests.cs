using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Social.Instagram.Tools;

namespace Daisi.SecureTools.Tests.Social;

public class InstagramPublishToolTests
{
    private static SocialHttpClient CreateSocialClient(SocialMockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task InstagramPublishTool_PublishesImage()
    {
        // 1. Get pages/IG account, 2. Create container, 3. Publish
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new { id = "page-1", instagram_business_account = new { id = "ig-123" } }
                }
            }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "container-456" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "media-789" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new InstagramPublishTool();

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "imageUrl", Value = "https://example.com/photo.jpg" },
            new ParameterValue { Name = "caption", Value = "Beautiful day!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("media-789", result.Output);
        Assert.Contains("\"type\":\"image\"", result.Output);
    }

    [Fact]
    public async Task InstagramPublishTool_RequiresMedia()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new InstagramPublishTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token", []);

        Assert.False(result.Success);
        Assert.Contains("imageUrl", result.ErrorMessage);
    }

    [Fact]
    public async Task InstagramPublishTool_ReturnsErrorWhenNoIgAccount()
    {
        var handler = new SocialMockHttpHandler(
            JsonSerializer.Serialize(new { data = Array.Empty<object>() }));

        var socialClient = CreateSocialClient(handler);
        var tool = new InstagramPublishTool();

        var result = await tool.ExecuteAsync(socialClient, "test-token",
            [new ParameterValue { Name = "imageUrl", Value = "https://example.com/photo.jpg" }]);

        Assert.False(result.Success);
        Assert.Contains("Instagram Business", result.ErrorMessage);
    }

    [Fact]
    public async Task InstagramPublishTool_PublishesCarousel()
    {
        var urls = JsonSerializer.Serialize(new[] { "https://example.com/1.jpg", "https://example.com/2.jpg" });

        // 1. Get IG account, 2. Create child 1, 3. Create child 2, 4. Create carousel container, 5. Publish
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new
            {
                data = new[] { new { id = "page-1", instagram_business_account = new { id = "ig-123" } } }
            }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "child-1" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "child-2" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "carousel-container" }), System.Net.HttpStatusCode.OK),
            (JsonSerializer.Serialize(new { id = "published-carousel" }), System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new InstagramPublishTool();

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "carouselUrls", Value = urls },
            new ParameterValue { Name = "caption", Value = "Carousel post!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("\"type\":\"carousel\"", result.Output);
    }
}
