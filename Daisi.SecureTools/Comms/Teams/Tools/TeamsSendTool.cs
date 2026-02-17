using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.Teams.Tools;

/// <summary>
/// Send a message to a Microsoft Teams chat via the Graph API.
/// Supports text and HTML content types.
/// </summary>
public class TeamsSendTool : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var chatId = parameters.FirstOrDefault(p => p.Name == "chatId")?.Value;
        if (string.IsNullOrEmpty(chatId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'chatId' parameter is required." };

        var message = parameters.FirstOrDefault(p => p.Name == "message")?.Value;
        if (string.IsNullOrEmpty(message))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'message' parameter is required." };

        var contentType = parameters.FirstOrDefault(p => p.Name == "contentType")?.Value ?? "text";

        var body = new
        {
            body = new
            {
                contentType,
                content = message
            }
        };

        var result = await httpClient.PostJsonAsync(
            $"https://graph.microsoft.com/v1.0/chats/{chatId}/messages",
            accessToken, body);

        var messageId = result.TryGetProperty("id", out var mid) ? mid.GetString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { messageId, chatId, contentType }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Message sent to Teams chat {chatId}."
        };
    }
}
