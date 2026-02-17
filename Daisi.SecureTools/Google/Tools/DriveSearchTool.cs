using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Search Google Drive for files matching a query.
/// Returns file id, name, mimeType, and modifiedTime.
/// </summary>
public class DriveSearchTool : IGoogleToolExecutor
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
            maxResults = Math.Clamp(parsed, 1, 100);

        var service = serviceFactory.CreateDriveService(accessToken);

        var listRequest = service.Files.List();
        listRequest.Q = $"name contains '{query.Replace("'", "\\'")}'";
        listRequest.PageSize = maxResults;
        listRequest.Fields = "files(id, name, mimeType, modifiedTime)";

        var listResponse = await listRequest.ExecuteAsync();

        if (listResponse.Files == null || listResponse.Files.Count == 0)
        {
            return new ExecuteResponse
            {
                Success = true,
                Output = "[]",
                OutputFormat = "json",
                OutputMessage = "No files found matching the query."
            };
        }

        var results = listResponse.Files.Select(f => new
        {
            id = f.Id,
            name = f.Name,
            mimeType = f.MimeType,
            modifiedTime = f.ModifiedTimeDateTimeOffset?.ToString("O") ?? ""
        }).ToList();

        var output = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Found {results.Count} file(s) matching '{query}'."
        };
    }
}
