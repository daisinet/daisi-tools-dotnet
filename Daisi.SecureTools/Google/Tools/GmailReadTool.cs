using System.Text;
using System.Text.Json;
using Google.Apis.Gmail.v1.Data;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Read a full email message by its message ID.
/// Returns subject, from, to, date, and body content.
/// </summary>
public class GmailReadTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var messageId = parameters.FirstOrDefault(p => p.Name == "messageId")?.Value;
        if (string.IsNullOrEmpty(messageId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'messageId' parameter is required." };

        var service = serviceFactory.CreateGmailService(accessToken);

        var message = await service.Users.Messages.Get("me", messageId).ExecuteAsync();
        var headers = message.Payload?.Headers;

        var subject = headers?.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(no subject)";
        var from = headers?.FirstOrDefault(h => h.Name == "From")?.Value ?? "(unknown)";
        var to = headers?.FirstOrDefault(h => h.Name == "To")?.Value ?? "(unknown)";
        var date = headers?.FirstOrDefault(h => h.Name == "Date")?.Value ?? "";

        var body = ExtractBody(message.Payload);

        var result = new
        {
            id = messageId,
            subject,
            from,
            to,
            date,
            body
        };

        var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Read message: {subject}"
        };
    }

    /// <summary>
    /// Extract body text from a message payload, preferring text/plain over text/html.
    /// </summary>
    internal static string ExtractBody(MessagePart? payload)
    {
        if (payload == null)
            return "";

        // Check for text/plain body directly
        if (payload.MimeType == "text/plain" && payload.Body?.Data != null)
            return DecodeBase64Url(payload.Body.Data);

        // Check for text/html body directly
        if (payload.MimeType == "text/html" && payload.Body?.Data != null)
            return DecodeBase64Url(payload.Body.Data);

        // Check parts for multipart messages
        if (payload.Parts != null)
        {
            // Prefer text/plain
            var plainPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
            if (plainPart?.Body?.Data != null)
                return DecodeBase64Url(plainPart.Body.Data);

            // Fallback to text/html
            var htmlPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/html");
            if (htmlPart?.Body?.Data != null)
                return DecodeBase64Url(htmlPart.Body.Data);

            // Recurse into nested parts
            foreach (var part in payload.Parts)
            {
                var nested = ExtractBody(part);
                if (!string.IsNullOrEmpty(nested))
                    return nested;
            }
        }

        return "";
    }

    internal static string DecodeBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }
}
