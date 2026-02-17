using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Social.TikTok.Tools;

namespace Daisi.SecureTools.Tests.Social;

public class TikTokPublishToolTests
{
    private static SocialHttpClient CreateSocialClient(SocialMockHttpHandler handler)
    {
        var factory = new MockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task TikTokPublishTool_RequiresMedia()
    {
        var handler = new SocialMockHttpHandler("{}");
        var tool = new TikTokPublishTool(new MockHttpClientFactory(handler));
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token", []);

        Assert.False(result.Success);
        Assert.Contains("videoUrl", result.ErrorMessage);
    }

    [Fact]
    public async Task TikTokPublishTool_PublishesVideo()
    {
        // Mock: video download, init upload, PUT upload
        var videoBytes = new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }; // fake mp4 header
        var videoBase64 = Convert.ToBase64String(videoBytes);

        var handler = new SocialMockHttpHandler(
            // Init upload response
            (JsonSerializer.Serialize(new
            {
                data = new
                {
                    publish_id = "pub-123",
                    upload_url = "https://upload.tiktok.com/video/123"
                }
            }), System.Net.HttpStatusCode.OK),
            // PUT upload response
            ("{}", System.Net.HttpStatusCode.OK)
        );

        var socialClient = CreateSocialClient(handler);
        var tool = new TikTokPublishTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "videoBase64", Value = videoBase64 },
            new ParameterValue { Name = "description", Value = "Cool video!" },
            new ParameterValue { Name = "privacy", Value = "SELF_ONLY" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("pub-123", result.Output);
        Assert.Contains("\"type\":\"video\"", result.Output);
        Assert.Contains("SELF_ONLY", result.Output);
    }

    [Fact]
    public async Task TikTokPublishTool_PublishesPhotoPost()
    {
        var photoUrls = JsonSerializer.Serialize(new[] { "https://example.com/1.jpg", "https://example.com/2.jpg" });

        var handler = new SocialMockHttpHandler(
            JsonSerializer.Serialize(new
            {
                data = new { publish_id = "photo-pub-456" }
            }));

        var socialClient = CreateSocialClient(handler);
        var tool = new TikTokPublishTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
        [
            new ParameterValue { Name = "photoUrls", Value = photoUrls },
            new ParameterValue { Name = "description", Value = "Photo post!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("photo-pub-456", result.Output);
        Assert.Contains("\"type\":\"photo\"", result.Output);
    }

    [Fact]
    public async Task TikTokPublishTool_DefaultsPrivacyToSelfOnly()
    {
        var handler = new SocialMockHttpHandler(
            (JsonSerializer.Serialize(new
            {
                data = new { publish_id = "pub-default", upload_url = "https://upload.tiktok.com/v" }
            }), System.Net.HttpStatusCode.OK),
            ("{}", System.Net.HttpStatusCode.OK)
        );

        var videoBase64 = Convert.ToBase64String(new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        var socialClient = CreateSocialClient(handler);
        var tool = new TikTokPublishTool(new MockHttpClientFactory(handler));

        var result = await tool.ExecuteAsync(socialClient, "test-token",
            [new ParameterValue { Name = "videoBase64", Value = videoBase64 }]);

        Assert.True(result.Success);
        Assert.Contains("SELF_ONLY", result.Output);
    }
}
