using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Search Gmail messages using Gmail query syntax.
/// Returns matching messages with subject, from, date, and snippet.
/// </summary>
public class GmailSearchTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var query = parameters.FirstOrDefault(p => p.Name == "query")?.Value;
        if (string.IsNullOrEmpty(query))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'query' parameter is required." };

        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;
        var maxResults = 10;
        if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            maxResults = Math.Clamp(parsed, 1, 50);

        var service = serviceFactory.CreateGmailService(accessToken);

        var listRequest = service.Users.Messages.List("me");
        listRequest.Q = query;
        listRequest.MaxResults = maxResults;

        var listResponse = await listRequest.ExecuteAsync();

        if (listResponse.Messages == null || listResponse.Messages.Count == 0)
        {
            return new ExecuteResponse
            {
                Success = true,
                Output = "[]",
                OutputFormat = "json",
                OutputMessage = "No messages found matching the query."
            };
        }

        var results = new List<object>();
        foreach (var msg in listResponse.Messages)
        {
            var detail = await service.Users.Messages.Get("me", msg.Id).ExecuteAsync();
            var headers = detail.Payload?.Headers;

            results.Add(new
            {
                id = msg.Id,
                subject = headers?.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(no subject)",
                from = headers?.FirstOrDefault(h => h.Name == "From")?.Value ?? "(unknown)",
                date = headers?.FirstOrDefault(h => h.Name == "Date")?.Value ?? "",
                snippet = detail.Snippet ?? ""
            });
        }

        var output = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Found {results.Count} message(s) matching '{query}'."
        };
    }
}
