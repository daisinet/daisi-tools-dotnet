using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social.LinkedIn.Tools;

/// <summary>
/// Post to LinkedIn. Supports text posts and image posts.
/// Uses LinkedIn's REST API with versioned headers.
/// </summary>
public class LinkedInPostTool(IHttpClientFactory httpClientFactory) : ISocialToolExecutor
{
    private const string ApiBase = "https://api.linkedin.com";

    private static readonly Dictionary<string, string> LinkedInHeaders = new()
    {
        ["LinkedIn-Version"] = "202402",
        ["X-Restli-Protocol-Version"] = "2.0.0"
    };

    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        if (string.IsNullOrEmpty(text))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'text' parameter is required." };

        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;
        var mediaBase64 = parameters.FirstOrDefault(p => p.Name == "mediaBase64")?.Value;
        var mediaTitle = parameters.FirstOrDefault(p => p.Name == "mediaTitle")?.Value;
        var visibility = parameters.FirstOrDefault(p => p.Name == "visibility")?.Value ?? "PUBLIC";

        // Get user URN
        var userInfo = await httpClient.GetJsonAsync($"{ApiBase}/v2/userinfo", accessToken);
        var sub = userInfo.GetProperty("sub").GetString();
        var authorUrn = $"urn:li:person:{sub}";

        // Build post body
        var postBody = new Dictionary<string, object>
        {
            ["author"] = authorUrn,
            ["lifecycleState"] = "PUBLISHED",
            ["visibility"] = visibility,
            ["commentary"] = text,
            ["distribution"] = new
            {
                feedDistribution = "MAIN_FEED",
                targetEntities = Array.Empty<object>(),
                thirdPartyDistributionChannels = Array.Empty<object>()
            }
        };

        // Upload image if provided
        var media = await MediaHelper.ResolveMediaAsync(httpClientFactory, mediaUrl, mediaBase64);
        if (media is not null)
        {
            var imageUrn = await UploadImageAsync(httpClient, accessToken, authorUrn,
                media.Value.Data, media.Value.ContentType);

            if (imageUrn is not null)
            {
                postBody["content"] = new
                {
                    media = new
                    {
                        id = imageUrn,
                        title = mediaTitle ?? "Image"
                    }
                };
            }
        }

        var result = await httpClient.PostJsonAsync(
            $"{ApiBase}/rest/posts", accessToken, postBody, LinkedInHeaders);

        var postUrn = result.TryGetProperty("id", out var id) ? id.GetString() : null;

        var output = JsonSerializer.Serialize(new
        {
            id = postUrn,
            author = authorUrn,
            visibility,
            hasMedia = media is not null
        }, SocialHttpClient.JsonOptions);

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = "Post published to LinkedIn."
        };
    }

    internal static async Task<string?> UploadImageAsync(
        SocialHttpClient httpClient, string accessToken, string ownerUrn,
        byte[] imageData, string contentType)
    {
        // Register upload
        var registerBody = new
        {
            initializeUploadRequest = new
            {
                owner = ownerUrn
            }
        };

        var registerResult = await httpClient.PostJsonAsync(
            $"{ApiBase}/rest/images?action=initializeUpload", accessToken, registerBody,
            LinkedInHeaders);

        if (!registerResult.TryGetProperty("value", out var value))
            return null;

        var uploadUrl = value.TryGetProperty("uploadUrl", out var u) ? u.GetString() : null;
        var imageUrn = value.TryGetProperty("image", out var img) ? img.GetString() : null;

        if (string.IsNullOrEmpty(uploadUrl) || string.IsNullOrEmpty(imageUrn))
            return null;

        // Upload binary
        await httpClient.PutBinaryAsync(uploadUrl, imageData, contentType);

        return imageUrn;
    }
}
