using System.Text.Json;
using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Search files in the user's OneDrive using the Microsoft Graph search endpoint.
/// Returns file id, name, web URL, size, and last modified date.
/// </summary>
public class OneDriveSearchTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var query = parameters.FirstOrDefault(p => p.Name == "query")?.Value;
        if (string.IsNullOrEmpty(query))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'query' parameter is required." };

        // Get the user's drive ID first, then search via the Drives collection
        var drive = await graphClient.Me.Drive.GetAsync();
        if (drive?.Id is null)
            return new ExecuteResponse { Success = false, ErrorMessage = "Unable to access OneDrive." };

        var searchResults = await graphClient.Drives[drive.Id].SearchWithQ(query).GetAsSearchWithQGetResponseAsync();

        var results = (searchResults?.Value ?? []).Select(item => new
        {
            id = item.Id,
            name = item.Name,
            webUrl = item.WebUrl,
            size = item.Size,
            lastModified = item.LastModifiedDateTime?.ToString("O"),
            mimeType = item.File?.MimeType
        });

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(results),
            OutputFormat = "json",
            OutputMessage = $"Found {results.Count()} files matching '{query}'"
        };
    }
}
