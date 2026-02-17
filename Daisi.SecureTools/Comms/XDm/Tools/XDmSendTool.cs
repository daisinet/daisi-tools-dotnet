using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.XDm.Tools;

/// <summary>
/// Send a direct message on X (Twitter) via the v2 API.
/// Supports text messages with optional media attachments.
/// </summary>
public class XDmSendTool(IHttpClientFactory httpClientFactory) : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var recipientId = parameters.FirstOrDefault(p => p.Name == "recipientId")?.Value;
        if (string.IsNullOrEmpty(recipientId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'recipientId' parameter is required." };

        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        if (string.IsNullOrEmpty(text))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'text' parameter is required." };

        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;
        var mediaBase64 = parameters.FirstOrDefault(p => p.Name == "mediaBase64")?.Value;

        // Upload media if provided
        string? mediaId = null;
        var media = await MediaHelper.ResolveMediaAsync(httpClientFactory, mediaUrl, mediaBase64);
        if (media is not null)
        {
            mediaId = await UploadDmMediaAsync(httpClient, accessToken, media.Value.Data, media.Value.ContentType);
        }

        var body = new Dictionary<string, object>
        {
            ["text"] = text
        };

        if (mediaId is not null)
        {
            body["attachments"] = new[] { new { media_id = mediaId } };
        }

        var result = await httpClient.PostJsonAsync(
            $"https://api.twitter.com/2/dm_conversations/with/{recipientId}/messages",
            accessToken, body);

        var dmEventId = result.TryGetProperty("data", out var data)
            && data.TryGetProperty("dm_event_id", out var eid)
            ? eid.GetString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { dmEventId, recipientId, hasMedia = mediaId is not null }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Direct message sent to user {recipientId}."
        };
    }

    private static async Task<string?> UploadDmMediaAsync(
        SocialHttpClient httpClient, string accessToken, byte[] data, string contentType)
    {
        // X v1.1 media upload for DM: INIT → APPEND → FINALIZE
        var initResult = await httpClient.PostFormUrlEncodedAsync(
            "https://upload.twitter.com/1.1/media/upload.json",
            new Dictionary<string, string>
            {
                ["command"] = "INIT",
                ["total_bytes"] = data.Length.ToString(),
                ["media_type"] = contentType,
                ["media_category"] = "dm_image"
            },
            bearerToken: accessToken);

        var mediaIdStr = initResult.GetProperty("media_id_string").GetString()!;

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("APPEND"), "command");
        form.Add(new StringContent(mediaIdStr), "media_id");
        form.Add(new StringContent("0"), "segment_index");
        form.Add(new ByteArrayContent(data), "media_data", "media");

        await httpClient.PostFormAsync(
            "https://upload.twitter.com/1.1/media/upload.json", accessToken, form);

        await httpClient.PostFormUrlEncodedAsync(
            "https://upload.twitter.com/1.1/media/upload.json",
            new Dictionary<string, string>
            {
                ["command"] = "FINALIZE",
                ["media_id"] = mediaIdStr
            },
            bearerToken: accessToken);

        return mediaIdStr;
    }
}
