using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social.X.Tools;

/// <summary>
/// Post a tweet to X (Twitter), with optional media, reply, or quote tweet.
/// </summary>
public class XPostTool(IHttpClientFactory httpClientFactory) : ISocialToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        if (string.IsNullOrEmpty(text))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'text' parameter is required." };

        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;
        var mediaBase64 = parameters.FirstOrDefault(p => p.Name == "mediaBase64")?.Value;
        var mediaAltText = parameters.FirstOrDefault(p => p.Name == "mediaAltText")?.Value;
        var replyToId = parameters.FirstOrDefault(p => p.Name == "replyToId")?.Value;
        var quoteTweetId = parameters.FirstOrDefault(p => p.Name == "quoteTweetId")?.Value;

        // Upload media if provided
        string? mediaId = null;
        var media = await MediaHelper.ResolveMediaAsync(httpClientFactory, mediaUrl, mediaBase64);
        if (media is not null)
        {
            mediaId = await UploadMediaAsync(httpClient, accessToken, media.Value.Data,
                media.Value.ContentType, mediaAltText);
        }

        // Build tweet payload
        var tweetBody = new Dictionary<string, object> { ["text"] = text };

        if (mediaId is not null)
        {
            tweetBody["media"] = new { media_ids = new[] { mediaId } };
        }

        if (!string.IsNullOrEmpty(replyToId))
        {
            tweetBody["reply"] = new { in_reply_to_tweet_id = replyToId };
        }

        if (!string.IsNullOrEmpty(quoteTweetId))
        {
            tweetBody["quote_tweet_id"] = quoteTweetId;
        }

        var result = await httpClient.PostJsonAsync(
            "https://api.twitter.com/2/tweets", accessToken, tweetBody);

        var tweetId = result.TryGetProperty("data", out var data)
            && data.TryGetProperty("id", out var id)
            ? id.GetString() : null;

        var output = JsonSerializer.Serialize(new
        {
            id = tweetId,
            text,
            hasMedia = mediaId is not null,
            isReply = !string.IsNullOrEmpty(replyToId),
            isQuote = !string.IsNullOrEmpty(quoteTweetId)
        }, SocialHttpClient.JsonOptions);

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Tweet posted successfully."
        };
    }

    internal static async Task<string?> UploadMediaAsync(
        SocialHttpClient httpClient, string accessToken, byte[] data, string contentType, string? altText)
    {
        // X v1.1 media upload: INIT
        var initResult = await httpClient.PostFormUrlEncodedAsync(
            "https://upload.twitter.com/1.1/media/upload.json",
            new Dictionary<string, string>
            {
                ["command"] = "INIT",
                ["total_bytes"] = data.Length.ToString(),
                ["media_type"] = contentType
            },
            bearerToken: accessToken);

        var mediaIdStr = initResult.GetProperty("media_id_string").GetString()!;

        // APPEND
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("APPEND"), "command");
        form.Add(new StringContent(mediaIdStr), "media_id");
        form.Add(new StringContent("0"), "segment_index");
        form.Add(new ByteArrayContent(data), "media_data", "media");

        await httpClient.PostFormAsync(
            "https://upload.twitter.com/1.1/media/upload.json", accessToken, form);

        // FINALIZE
        await httpClient.PostFormUrlEncodedAsync(
            "https://upload.twitter.com/1.1/media/upload.json",
            new Dictionary<string, string>
            {
                ["command"] = "FINALIZE",
                ["media_id"] = mediaIdStr
            },
            bearerToken: accessToken);

        // Alt text
        if (!string.IsNullOrEmpty(altText))
        {
            await httpClient.PostJsonAsync(
                "https://api.twitter.com/1.1/media/metadata/create.json", accessToken,
                new { media_id = mediaIdStr, alt_text = new { text = altText } });
        }

        return mediaIdStr;
    }
}
