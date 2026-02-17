using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Send a message to a Microsoft Teams channel using the Microsoft Graph API.
/// Requires teamId and channelId to identify the target channel.
/// </summary>
public class TeamsSendTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var teamId = parameters.FirstOrDefault(p => p.Name == "teamId")?.Value;
        var channelId = parameters.FirstOrDefault(p => p.Name == "channelId")?.Value;
        var content = parameters.FirstOrDefault(p => p.Name == "content")?.Value;

        if (string.IsNullOrEmpty(teamId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'teamId' parameter is required." };
        if (string.IsNullOrEmpty(channelId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'channelId' parameter is required." };
        if (string.IsNullOrEmpty(content))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'content' parameter is required." };

        var contentTypeParam = parameters.FirstOrDefault(p => p.Name == "contentType")?.Value;
        var bodyContentType = contentTypeParam?.Equals("html", StringComparison.OrdinalIgnoreCase) == true
            ? BodyType.Html
            : BodyType.Text;

        var chatMessage = new ChatMessage
        {
            Body = new ItemBody
            {
                ContentType = bodyContentType,
                Content = content
            }
        };

        var sent = await graphClient.Teams[teamId].Channels[channelId].Messages.PostAsync(chatMessage);

        var result = new
        {
            id = sent?.Id,
            teamId,
            channelId,
            sent = true,
            createdDateTime = sent?.CreatedDateTime?.ToString("O")
        };

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(result),
            OutputFormat = "json",
            OutputMessage = "Message sent to Teams channel"
        };
    }
}
