using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// List unread messages in the Gmail inbox.
/// Returns unread messages with subject, from, date, and snippet.
/// </summary>
public class GmailUnreadTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;
        var maxResults = 10;
        if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            maxResults = Math.Clamp(parsed, 1, 50);

        var service = serviceFactory.CreateGmailService(accessToken);

        var listRequest = service.Users.Messages.List("me");
        listRequest.Q = "is:unread in:inbox";
        listRequest.MaxResults = maxResults;

        var listResponse = await listRequest.ExecuteAsync();

        if (listResponse.Messages == null || listResponse.Messages.Count == 0)
        {
            return new ExecuteResponse
            {
                Success = true,
                Output = "[]",
                OutputFormat = "json",
                OutputMessage = "No unread messages in inbox."
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
            OutputMessage = $"Found {results.Count} unread message(s)."
        };
    }
}
