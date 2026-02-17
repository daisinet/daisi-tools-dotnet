using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social.TikTok.Tools;

/// <summary>
/// Publish a video or photo post to TikTok.
/// Uses TikTok's Content Posting API with direct upload.
/// Note: Unaudited apps can only post as PRIVATE visibility.
/// </summary>
public class TikTokPublishTool(IHttpClientFactory httpClientFactory) : ISocialToolExecutor
{
    private const string ApiBase = "https://open.tiktokapis.com/v2";

    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var description = parameters.FirstOrDefault(p => p.Name == "description")?.Value;
        var videoUrl = parameters.FirstOrDefault(p => p.Name == "videoUrl")?.Value;
        var videoBase64 = parameters.FirstOrDefault(p => p.Name == "videoBase64")?.Value;
        var photoUrlsParam = parameters.FirstOrDefault(p => p.Name == "photoUrls")?.Value;
        var privacy = parameters.FirstOrDefault(p => p.Name == "privacy")?.Value ?? "SELF_ONLY";
        var disableComment = parameters.FirstOrDefault(p => p.Name == "disableComment")?.Value;
        var disableDuet = parameters.FirstOrDefault(p => p.Name == "disableDuet")?.Value;

        if (string.IsNullOrEmpty(videoUrl) && string.IsNullOrEmpty(videoBase64) && string.IsNullOrEmpty(photoUrlsParam))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "At least one of 'videoUrl', 'videoBase64', or 'photoUrls' is required."
            };
        }

        // Photo post
        if (!string.IsNullOrEmpty(photoUrlsParam))
        {
            string[] photoUrls;
            try { photoUrls = JsonSerializer.Deserialize<string[]>(photoUrlsParam) ?? []; }
            catch { return new ExecuteResponse { Success = false, ErrorMessage = "Invalid 'photoUrls' format. Provide a JSON array of URLs." }; }

            return await PublishPhotoPostAsync(httpClient, accessToken, photoUrls, description, privacy,
                disableComment, disableDuet);
        }

        // Video post
        var media = await MediaHelper.ResolveMediaAsync(httpClientFactory, videoUrl, videoBase64);
        if (media is null)
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Could not resolve video content."
            };
        }

        return await PublishVideoAsync(httpClient, accessToken, media.Value.Data,
            description, privacy, disableComment, disableDuet);
    }

    internal static async Task<ExecuteResponse> PublishVideoAsync(
        SocialHttpClient httpClient, string accessToken, byte[] videoData,
        string? description, string privacy, string? disableComment, string? disableDuet)
    {
        // Init upload
        var initBody = new
        {
            post_info = new
            {
                title = description ?? "",
                privacy_level = privacy,
                disable_comment = disableComment == "true",
                disable_duet = disableDuet == "true"
            },
            source_info = new
            {
                source = "FILE_UPLOAD",
                video_size = videoData.Length,
                chunk_size = videoData.Length,
                total_chunk_count = 1
            }
        };

        var initResult = await httpClient.PostJsonAsync(
            $"{ApiBase}/post/publish/video/init/", accessToken, initBody);

        if (!initResult.TryGetProperty("data", out var data)
            || !data.TryGetProperty("upload_url", out var uploadUrlProp))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Failed to initialize TikTok video upload."
            };
        }

        var uploadUrl = uploadUrlProp.GetString()!;
        var publishId = data.TryGetProperty("publish_id", out var pid) ? pid.GetString() : null;

        // Upload video
        var uploadHeaders = new Dictionary<string, string>
        {
            ["Content-Range"] = $"bytes 0-{videoData.Length - 1}/{videoData.Length}"
        };
        await httpClient.PutBinaryAsync(uploadUrl, videoData, "video/mp4", uploadHeaders);

        var output = JsonSerializer.Serialize(new
        {
            publishId,
            type = "video",
            privacy,
            status = "processing"
        }, SocialHttpClient.JsonOptions);

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = "Video uploaded to TikTok and is being processed."
        };
    }

    internal static async Task<ExecuteResponse> PublishPhotoPostAsync(
        SocialHttpClient httpClient, string accessToken, string[] photoUrls,
        string? description, string privacy, string? disableComment, string? disableDuet)
    {
        var postBody = new
        {
            post_info = new
            {
                title = description ?? "",
                privacy_level = privacy,
                disable_comment = disableComment == "true",
                disable_duet = disableDuet == "true"
            },
            source_info = new
            {
                source = "PULL_FROM_URL",
                photo_cover_index = 0,
                photo_images = photoUrls
            },
            media_type = "PHOTO"
        };

        var result = await httpClient.PostJsonAsync(
            $"{ApiBase}/post/publish/content/init/", accessToken, postBody);

        var publishId = result.TryGetProperty("data", out var data)
            && data.TryGetProperty("publish_id", out var pid) ? pid.GetString() : null;

        var output = JsonSerializer.Serialize(new
        {
            publishId,
            type = "photo",
            photoCount = photoUrls.Length,
            privacy,
            status = "processing"
        }, SocialHttpClient.JsonOptions);

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = "Photo post uploaded to TikTok and is being processed."
        };
    }
}
