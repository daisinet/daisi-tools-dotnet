using System.Text.Json;
using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Search Outlook mail using the Microsoft Graph $search query parameter.
/// Returns matching messages with subject, from, date, and preview.
/// </summary>
public class MailSearchTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var query = parameters.FirstOrDefault(p => p.Name == "query")?.Value;
        if (string.IsNullOrEmpty(query))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'query' parameter is required." };

        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;
        var maxResults = 10;
        if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            maxResults = Math.Clamp(parsed, 1, 50);

        var messages = await graphClient.Me.Messages.GetAsync(config =>
        {
            config.QueryParameters.Search = $"\"{query}\"";
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
            OutputMessage = $"Found {results.Count()} messages matching '{query}'"
        };
    }
}
