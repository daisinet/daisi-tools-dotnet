using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.Telegram.Tools;

/// <summary>
/// Send a message to a Telegram chat via the Bot API.
/// Supports text messages, photos, documents, and videos.
/// </summary>
public class TelegramSendTool(IHttpClientFactory httpClientFactory) : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        // accessToken is the bot token
        var chatId = parameters.FirstOrDefault(p => p.Name == "chatId")?.Value;
        if (string.IsNullOrEmpty(chatId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'chatId' parameter is required." };

        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;
        var mediaBase64 = parameters.FirstOrDefault(p => p.Name == "mediaBase64")?.Value;
        var parseMode = parameters.FirstOrDefault(p => p.Name == "parseMode")?.Value;

        var botApiBase = $"https://api.telegram.org/bot{accessToken}";

        // If media is provided, send as photo/document/video
        var media = await MediaHelper.ResolveMediaAsync(httpClientFactory, mediaUrl, mediaBase64);
        if (media is not null)
        {
            return await SendMediaMessageAsync(httpClient, botApiBase, chatId, text, parseMode,
                media.Value.Data, media.Value.ContentType);
        }

        // Text-only message
        if (string.IsNullOrEmpty(text))
            return new ExecuteResponse { Success = false, ErrorMessage = "Either 'text' or media must be provided." };

        var body = new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            ["text"] = text
        };

        if (!string.IsNullOrEmpty(parseMode))
            body["parse_mode"] = parseMode;

        // Telegram Bot API doesn't use Bearer auth — token is in the URL path
        var result = await httpClient.PostJsonAsync($"{botApiBase}/sendMessage", "", body);

        var messageId = result.TryGetProperty("result", out var r)
            && r.TryGetProperty("message_id", out var mid)
            ? mid.GetInt32().ToString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { messageId, chatId, type = "text" }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Message sent to chat {chatId}."
        };
    }

    private static async Task<ExecuteResponse> SendMediaMessageAsync(
        SocialHttpClient httpClient, string botApiBase, string chatId, string? caption,
        string? parseMode, byte[] data, string contentType)
    {
        // Determine the appropriate Telegram method based on content type
        var method = contentType switch
        {
            "video/mp4" => "sendVideo",
            "image/jpeg" or "image/png" or "image/gif" or "image/webp" => "sendPhoto",
            _ => "sendDocument"
        };

        var fieldName = method switch
        {
            "sendVideo" => "video",
            "sendPhoto" => "photo",
            _ => "document"
        };

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(chatId), "chat_id");

        if (!string.IsNullOrEmpty(caption))
            form.Add(new StringContent(caption), "caption");

        if (!string.IsNullOrEmpty(parseMode))
            form.Add(new StringContent(parseMode), "parse_mode");

        var extension = contentType.Split('/').LastOrDefault() ?? "bin";
        form.Add(new ByteArrayContent(data), fieldName, $"file.{extension}");

        var result = await httpClient.PostFormAsync($"{botApiBase}/{method}", "", form);

        var messageId = result.TryGetProperty("result", out var r)
            && r.TryGetProperty("message_id", out var mid)
            ? mid.GetInt32().ToString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { messageId, chatId, type = fieldName }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Media message sent to chat {chatId}."
        };
    }
}
