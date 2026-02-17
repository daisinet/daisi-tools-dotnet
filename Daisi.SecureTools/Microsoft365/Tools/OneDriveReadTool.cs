using System.Text.Json;
using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Download and read a file from OneDrive by item ID.
/// Returns the content as text for text-based files, or indicates binary type for others.
/// </summary>
public class OneDriveReadTool : IGraphToolExecutor
{
    /// <summary>
    /// Maximum file size in bytes to attempt reading as text (5 MB).
    /// </summary>
    private const long MaxTextFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> TextMimeTypes =
    [
        "text/plain", "text/html", "text/css", "text/csv", "text/xml",
        "application/json", "application/xml", "application/javascript",
        "text/markdown", "text/yaml", "application/x-yaml"
    ];

    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var itemId = parameters.FirstOrDefault(p => p.Name == "itemId")?.Value;
        if (string.IsNullOrEmpty(itemId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'itemId' parameter is required." };

        // Get the user's drive ID first, then access items via the Drives collection
        var drive = await graphClient.Me.Drive.GetAsync();
        if (drive?.Id is null)
            return new ExecuteResponse { Success = false, ErrorMessage = "Unable to access OneDrive." };

        // Get file metadata first
        var driveItem = await graphClient.Drives[drive.Id].Items[itemId].GetAsync();
        if (driveItem is null)
            return new ExecuteResponse { Success = false, ErrorMessage = $"File '{itemId}' not found." };

        var mimeType = driveItem.File?.MimeType ?? "application/octet-stream";
        var isText = TextMimeTypes.Contains(mimeType) ||
                     mimeType.StartsWith("text/") ||
                     (driveItem.Name?.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (driveItem.Name?.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ?? false);

        if (!isText)
        {
            var metaResult = new
            {
                id = driveItem.Id,
                name = driveItem.Name,
                mimeType,
                size = driveItem.Size,
                webUrl = driveItem.WebUrl,
                message = "This is a binary file and cannot be displayed as text. Use the webUrl to access it."
            };

            return new ExecuteResponse
            {
                Success = true,
                Output = JsonSerializer.Serialize(metaResult),
                OutputFormat = "json",
                OutputMessage = $"File '{driveItem.Name}' is a binary file ({mimeType})"
            };
        }

        if (driveItem.Size > MaxTextFileSize)
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = $"File '{driveItem.Name}' is too large to read ({driveItem.Size} bytes). Maximum is {MaxTextFileSize} bytes."
            };
        }

        // Download the content
        var stream = await graphClient.Drives[drive.Id].Items[itemId].Content.GetAsync();
        if (stream is null)
            return new ExecuteResponse { Success = false, ErrorMessage = "Failed to download file content." };

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        var result = new
        {
            id = driveItem.Id,
            name = driveItem.Name,
            mimeType,
            size = driveItem.Size,
            content
        };

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(result),
            OutputFormat = "json",
            OutputMessage = $"Read file: {driveItem.Name} ({driveItem.Size} bytes)"
        };
    }
}
