using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social.Instagram.Tools;

/// <summary>
/// Publish content to Instagram. Supports single image, single video, and carousel posts.
/// Media must be at publicly accessible URLs (no binary upload).
/// Requires a Business or Creator Instagram account.
/// </summary>
public class InstagramPublishTool : ISocialToolExecutor
{
    private const string GraphApiBase = "https://graph.facebook.com/v22.0";

    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var caption = parameters.FirstOrDefault(p => p.Name == "caption")?.Value;
        var imageUrl = parameters.FirstOrDefault(p => p.Name == "imageUrl")?.Value;
        var videoUrl = parameters.FirstOrDefault(p => p.Name == "videoUrl")?.Value;
        var carouselUrlsParam = parameters.FirstOrDefault(p => p.Name == "carouselUrls")?.Value;
        var mediaType = parameters.FirstOrDefault(p => p.Name == "mediaType")?.Value ?? "IMAGE";

        if (string.IsNullOrEmpty(imageUrl) && string.IsNullOrEmpty(videoUrl) && string.IsNullOrEmpty(carouselUrlsParam))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "At least one of 'imageUrl', 'videoUrl', or 'carouselUrls' is required."
            };
        }

        // Get Instagram Business Account ID from the user's pages
        var igAccountId = await GetInstagramAccountIdAsync(httpClient, accessToken);
        if (igAccountId is null)
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Could not find an Instagram Business/Creator account. Ensure a page is linked."
            };
        }

        string? creationId;

        // Carousel post
        if (!string.IsNullOrEmpty(carouselUrlsParam))
        {
            string[] carouselUrls;
            try { carouselUrls = JsonSerializer.Deserialize<string[]>(carouselUrlsParam) ?? []; }
            catch { return new ExecuteResponse { Success = false, ErrorMessage = "Invalid 'carouselUrls' format. Provide a JSON array of URLs." }; }

            var childIds = new List<string>();
            foreach (var url in carouselUrls)
            {
                var childBody = new Dictionary<string, object>
                {
                    ["image_url"] = url,
                    ["is_carousel_item"] = true
                };
                var childResult = await httpClient.PostJsonAsync(
                    $"{GraphApiBase}/{igAccountId}/media", accessToken, childBody);
                var childId = childResult.GetProperty("id").GetString()!;
                childIds.Add(childId);
            }

            var carouselBody = new Dictionary<string, object>
            {
                ["media_type"] = "CAROUSEL",
                ["children"] = childIds
            };
            if (!string.IsNullOrEmpty(caption))
                carouselBody["caption"] = caption;

            var containerResult = await httpClient.PostJsonAsync(
                $"{GraphApiBase}/{igAccountId}/media", accessToken, carouselBody);
            creationId = containerResult.GetProperty("id").GetString();
        }
        // Video post
        else if (!string.IsNullOrEmpty(videoUrl))
        {
            var containerBody = new Dictionary<string, object>
            {
                ["media_type"] = "REELS",
                ["video_url"] = videoUrl
            };
            if (!string.IsNullOrEmpty(caption))
                containerBody["caption"] = caption;

            var containerResult = await httpClient.PostJsonAsync(
                $"{GraphApiBase}/{igAccountId}/media", accessToken, containerBody);
            creationId = containerResult.GetProperty("id").GetString();

            // Poll until container is ready
            await PollContainerStatusAsync(httpClient, accessToken, creationId!);
        }
        // Image post
        else
        {
            var containerBody = new Dictionary<string, object>
            {
                ["image_url"] = imageUrl!
            };
            if (!string.IsNullOrEmpty(caption))
                containerBody["caption"] = caption;

            var containerResult = await httpClient.PostJsonAsync(
                $"{GraphApiBase}/{igAccountId}/media", accessToken, containerBody);
            creationId = containerResult.GetProperty("id").GetString();
        }

        // Publish the container
        var publishResult = await httpClient.PostJsonAsync(
            $"{GraphApiBase}/{igAccountId}/media_publish", accessToken,
            new { creation_id = creationId });

        var mediaId = publishResult.TryGetProperty("id", out var id) ? id.GetString() : null;

        var output = JsonSerializer.Serialize(new
        {
            id = mediaId,
            igAccountId,
            type = !string.IsNullOrEmpty(carouselUrlsParam) ? "carousel"
                : !string.IsNullOrEmpty(videoUrl) ? "video" : "image"
        }, SocialHttpClient.JsonOptions);

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = "Content published to Instagram."
        };
    }

    internal static async Task<string?> GetInstagramAccountIdAsync(
        SocialHttpClient httpClient, string accessToken)
    {
        // Get user's pages, then find the Instagram Business Account
        var pagesResult = await httpClient.GetJsonAsync(
            $"{GraphApiBase}/me/accounts?fields=instagram_business_account", accessToken);

        if (!pagesResult.TryGetProperty("data", out var data))
            return null;

        foreach (var page in data.EnumerateArray())
        {
            if (page.TryGetProperty("instagram_business_account", out var igAccount)
                && igAccount.TryGetProperty("id", out var igId))
            {
                return igId.GetString();
            }
        }

        return null;
    }

    internal static async Task PollContainerStatusAsync(
        SocialHttpClient httpClient, string accessToken, string containerId, int maxAttempts = 30)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(2000);

            var status = await httpClient.GetJsonAsync(
                $"{GraphApiBase}/{containerId}?fields=status_code", accessToken);

            if (status.TryGetProperty("status_code", out var code))
            {
                var statusStr = code.GetString();
                if (statusStr == "FINISHED")
                    return;
                if (statusStr == "ERROR")
                    throw new SocialApiException("Instagram container processing failed.");
            }
        }

        throw new SocialApiException("Instagram container processing timed out.");
    }
}
