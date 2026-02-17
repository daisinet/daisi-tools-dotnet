using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social.Facebook.Tools;

/// <summary>
/// Post to a Facebook Page. Supports text posts, photo posts, and link shares.
/// Cannot post to personal profiles — Pages only.
/// </summary>
public class FacebookPostTool : ISocialToolExecutor
{
    private const string GraphApiBase = "https://graph.facebook.com/v22.0";

    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var pageId = parameters.FirstOrDefault(p => p.Name == "pageId")?.Value;
        if (string.IsNullOrEmpty(pageId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'pageId' parameter is required." };

        var message = parameters.FirstOrDefault(p => p.Name == "message")?.Value;
        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;
        var link = parameters.FirstOrDefault(p => p.Name == "link")?.Value;

        // Get page access token from user token
        var pageToken = await GetPageAccessTokenAsync(httpClient, accessToken, pageId);
        if (pageToken is null)
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = $"Could not get access token for page {pageId}. Ensure the page is authorized."
            };
        }

        // Photo post
        if (!string.IsNullOrEmpty(mediaUrl))
        {
            var photoBody = new Dictionary<string, object> { ["url"] = mediaUrl };
            if (!string.IsNullOrEmpty(message))
                photoBody["caption"] = message;

            var photoResult = await httpClient.PostJsonAsync(
                $"{GraphApiBase}/{pageId}/photos", pageToken, photoBody);

            var photoId = photoResult.TryGetProperty("id", out var pid) ? pid.GetString() : null;

            return new ExecuteResponse
            {
                Success = true,
                Output = JsonSerializer.Serialize(new { id = photoId, type = "photo", pageId }, SocialHttpClient.JsonOptions),
                OutputFormat = "json",
                OutputMessage = $"Photo posted to page {pageId}."
            };
        }

        // Text or link post
        var postBody = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(message))
            postBody["message"] = message;
        if (!string.IsNullOrEmpty(link))
            postBody["link"] = link;

        if (postBody.Count == 0)
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "At least one of 'message', 'mediaUrl', or 'link' must be provided."
            };
        }

        var result = await httpClient.PostJsonAsync(
            $"{GraphApiBase}/{pageId}/feed", pageToken, postBody);

        var postId = result.TryGetProperty("id", out var id) ? id.GetString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { id = postId, type = "post", pageId }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Post published to page {pageId}."
        };
    }

    internal static async Task<string?> GetPageAccessTokenAsync(
        SocialHttpClient httpClient, string userAccessToken, string pageId)
    {
        var result = await httpClient.GetJsonAsync(
            $"{GraphApiBase}/{pageId}?fields=access_token", userAccessToken);

        return result.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }
}
