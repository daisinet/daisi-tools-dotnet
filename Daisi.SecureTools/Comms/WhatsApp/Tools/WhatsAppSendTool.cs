using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.WhatsApp.Tools;

/// <summary>
/// Send a WhatsApp message via the Meta Cloud API.
/// Supports text messages, template messages, and media messages.
/// </summary>
public class WhatsAppSendTool : ICommsToolExecutor
{
    private const string GraphApiBase = "https://graph.facebook.com/v22.0";

    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var phoneNumberId = parameters.FirstOrDefault(p => p.Name == "phoneNumberId")?.Value;
        if (string.IsNullOrEmpty(phoneNumberId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'phoneNumberId' parameter is required." };

        var to = parameters.FirstOrDefault(p => p.Name == "to")?.Value;
        if (string.IsNullOrEmpty(to))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'to' parameter is required." };

        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        var templateName = parameters.FirstOrDefault(p => p.Name == "templateName")?.Value;
        var templateLanguage = parameters.FirstOrDefault(p => p.Name == "templateLanguage")?.Value ?? "en_US";
        var templateParams = parameters.FirstOrDefault(p => p.Name == "templateParams")?.Value;
        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;

        var body = new Dictionary<string, object>
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = to
        };

        string messageType;

        if (!string.IsNullOrEmpty(templateName))
        {
            // Template message
            messageType = "template";
            var template = new Dictionary<string, object>
            {
                ["name"] = templateName,
                ["language"] = new { code = templateLanguage }
            };

            if (!string.IsNullOrEmpty(templateParams))
            {
                var paramValues = JsonSerializer.Deserialize<string[]>(templateParams) ?? [];
                var components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = paramValues.Select(v => new { type = "text", text = v }).ToArray()
                    }
                };
                template["components"] = components;
            }

            body["type"] = "template";
            body["template"] = template;
        }
        else if (!string.IsNullOrEmpty(mediaUrl))
        {
            // Media message (image)
            messageType = "image";
            body["type"] = "image";
            body["image"] = new { link = mediaUrl, caption = text ?? "" };
        }
        else if (!string.IsNullOrEmpty(text))
        {
            // Text message
            messageType = "text";
            body["type"] = "text";
            body["text"] = new { preview_url = false, body = text };
        }
        else
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "At least one of 'text', 'templateName', or 'mediaUrl' must be provided."
            };
        }

        var result = await httpClient.PostJsonAsync(
            $"{GraphApiBase}/{phoneNumberId}/messages", accessToken, body);

        var messageId = result.TryGetProperty("messages", out var msgs)
            && msgs.GetArrayLength() > 0
            && msgs[0].TryGetProperty("id", out var mid)
            ? mid.GetString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { messageId, to, type = messageType }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"WhatsApp {messageType} message sent to {to}."
        };
    }
}
