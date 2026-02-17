using System.Text;
using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Read a file's content from Google Drive by file ID.
/// For Google Docs/Sheets/Slides, exports as plain text.
/// For binary files, downloads and returns base64-encoded content.
/// </summary>
public class DriveReadTool : IGoogleToolExecutor
{
    private static readonly Dictionary<string, string> GoogleDocExportMimeTypes = new()
    {
        ["application/vnd.google-apps.document"] = "text/plain",
        ["application/vnd.google-apps.spreadsheet"] = "text/csv",
        ["application/vnd.google-apps.presentation"] = "text/plain",
    };

    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var fileId = parameters.FirstOrDefault(p => p.Name == "fileId")?.Value;
        if (string.IsNullOrEmpty(fileId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'fileId' parameter is required." };

        var service = serviceFactory.CreateDriveService(accessToken);

        // Get file metadata first
        var getRequest = service.Files.Get(fileId);
        getRequest.Fields = "id, name, mimeType, size";
        var file = await getRequest.ExecuteAsync();

        string content;
        string outputFormat;

        if (GoogleDocExportMimeTypes.TryGetValue(file.MimeType, out var exportMimeType))
        {
            // Export Google Workspace files
            var exportRequest = service.Files.Export(fileId, exportMimeType);
            using var stream = new MemoryStream();
            await exportRequest.DownloadAsync(stream);
            stream.Position = 0;
            content = Encoding.UTF8.GetString(stream.ToArray());
            outputFormat = "plaintext";
        }
        else
        {
            // Download binary files
            var downloadRequest = service.Files.Get(fileId);
            using var stream = new MemoryStream();
            await downloadRequest.DownloadAsync(stream);
            stream.Position = 0;

            // Try to read as text; fall back to base64
            if (IsTextMimeType(file.MimeType))
            {
                content = Encoding.UTF8.GetString(stream.ToArray());
                outputFormat = "plaintext";
            }
            else
            {
                content = Convert.ToBase64String(stream.ToArray());
                outputFormat = "base64";
            }
        }

        var result = new
        {
            id = file.Id,
            name = file.Name,
            mimeType = file.MimeType,
            format = outputFormat,
            content
        };

        var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = outputFormat == "base64" ? "json" : "plaintext",
            OutputMessage = $"Read file: {file.Name} ({file.MimeType})"
        };
    }

    private static bool IsTextMimeType(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return false;

        return mimeType.StartsWith("text/") ||
               mimeType == "application/json" ||
               mimeType == "application/xml" ||
               mimeType == "application/javascript";
    }
}
