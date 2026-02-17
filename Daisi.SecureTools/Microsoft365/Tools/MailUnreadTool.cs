using System.Text.Json;
using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Get unread emails from the user's Outlook inbox using isRead eq false filter.
/// Returns messages with subject, from, date, and preview.
/// </summary>
public class MailUnreadTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;
        var maxResults = 10;
        if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            maxResults = Math.Clamp(parsed, 1, 50);

        var messages = await graphClient.Me.Messages.GetAsync(config =>
        {
            config.QueryParameters.Filter = "isRead eq false";
            config.QueryParameters.Top = maxResults;
            config.QueryParameters.Select = ["subject", "from", "receivedDateTime", "bodyPreview"];
            config.QueryParameters.Orderby = ["receivedDateTime desc"];
        });

        var results = (messages?.Value ?? []).Select(m => new
        {
            id = m.Id,
            subject = m.Subject,
            from = m.From?.EmailAddress?.Address,
            fromName = m.From?.EmailAddress?.Name,
            date = m.ReceivedDateTime?.ToString("O"),
            preview = m.BodyPreview
        });

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(results),
            OutputFormat = "json",
            OutputMessage = $"Found {results.Count()} unread messages"
        };
    }
}
