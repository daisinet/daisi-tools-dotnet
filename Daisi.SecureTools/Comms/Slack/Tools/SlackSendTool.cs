using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.Slack.Tools;

/// <summary>
/// Send a message to a Slack channel via the Slack Web API.
/// Supports text messages, threading, and media attachments.
/// </summary>
public class SlackSendTool(IHttpClientFactory httpClientFactory) : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var channel = parameters.FirstOrDefault(p => p.Name == "channel")?.Value;
        if (string.IsNullOrEmpty(channel))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'channel' parameter is required." };

        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        if (string.IsNullOrEmpty(text))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'text' parameter is required." };

        var threadTs = parameters.FirstOrDefault(p => p.Name == "threadTs")?.Value;
        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;
        var mediaBase64 = parameters.FirstOrDefault(p => p.Name == "mediaBase64")?.Value;

        // Upload file if media is provided
        string? fileId = null;
        var media = await MediaHelper.ResolveMediaAsync(httpClientFactory, mediaUrl, mediaBase64);
        if (media is not null)
        {
            fileId = await UploadFileAsync(httpClient, accessToken, channel, media.Value.Data, media.Value.ContentType);
        }

        // Post the message
        var body = new Dictionary<string, object>
        {
            ["channel"] = channel,
            ["text"] = text
        };

        if (!string.IsNullOrEmpty(threadTs))
            body["thread_ts"] = threadTs;

        var result = await httpClient.PostJsonAsync(
            "https://slack.com/api/chat.postMessage", accessToken, body);

        var ts = result.TryGetProperty("ts", out var tsVal) ? tsVal.GetString() : null;
        var ok = result.TryGetProperty("ok", out var okVal) && okVal.GetBoolean();

        if (!ok)
        {
            var error = result.TryGetProperty("error", out var errVal) ? errVal.GetString() : "Unknown error";
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = $"Slack API error: {error}"
            };
        }

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new
            {
                ts,
                channel,
                threadTs,
                hasFile = fileId is not null
            }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Message sent to Slack channel {channel}."
        };
    }

    private static async Task<string?> UploadFileAsync(
        SocialHttpClient httpClient, string accessToken, string channel,
        byte[] data, string contentType)
    {
        var extension = contentType.Split('/').LastOrDefault() ?? "bin";

        // Step 1: Get upload URL
        var getUrlResult = await httpClient.GetJsonAsync(
            $"https://slack.com/api/files.getUploadURLExternal?filename=file.{extension}&length={data.Length}",
            accessToken);

        var uploadUrl = getUrlResult.TryGetProperty("upload_url", out var uUrl) ? uUrl.GetString() : null;
        var fileIdVal = getUrlResult.TryGetProperty("file_id", out var fId) ? fId.GetString() : null;

        if (uploadUrl is null || fileIdVal is null)
            return null;

        // Step 2: Upload the file
        using var uploadContent = new ByteArrayContent(data);
        uploadContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var client = new HttpClient();
        var uploadResponse = await client.PostAsync(uploadUrl, uploadContent);
        uploadResponse.EnsureSuccessStatusCode();

        // Step 3: Complete the upload
        await httpClient.PostJsonAsync(
            "https://slack.com/api/files.completeUploadExternal", accessToken,
            new
            {
                files = new[] { new { id = fileIdVal, title = $"file.{extension}" } },
                channel_id = channel
            });

        return fileIdVal;
    }
}
