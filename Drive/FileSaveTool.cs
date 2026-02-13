using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text;
using System.Text.Json;

namespace Daisi.Tools.Drive
{
    public class FileSaveTool : DriveToolBase
    {
        private const string P_FILE_NAME = "file-name";
        private const string P_CONTENT = "content";
        private const string P_PATH = "path";
        private const string P_IS_SYSTEM = "is-system";
        private const string P_REPOSITORY_ID = "repository-id";
        private const string P_FOLDER_ID = "folder-id";

        public override string Id => "daisi-drive-save";
        public override string Name => "Daisi Drive Save File";

        public override string UseInstructions =>
            "Use this tool to save text content as a new file in Daisi Drive. " +
            "Always confirm with the user before creating or modifying files. " +
            "Use is-system=true for AI internal files like memory, research cache, preferences. " +
            "Optionally target a specific repository and folder using repository-id and folder-id.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_FILE_NAME,
                Description = "The name for the file (e.g., 'report.md', 'notes.txt').",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_CONTENT,
                Description = "The text content to save in the file.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_PATH,
                Description = "The folder path in Drive (e.g., '/documents/reports'). Default is '/'.",
                IsRequired = false
            },
            new ToolParameter(){
                Name = P_IS_SYSTEM,
                Description = "Set to 'true' for system files hidden from the user. Default is 'false'.",
                IsRequired = false
            },
            new ToolParameter(){
                Name = P_REPOSITORY_ID,
                Description = "Optional repository ID to save the file to. Defaults to Account repository.",
                IsRequired = false
            },
            new ToolParameter(){
                Name = P_FOLDER_ID,
                Description = "Optional folder ID within the repository. Defaults to root.",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var fileName = parameters.GetParameter(P_FILE_NAME).Value;
            var content = parameters.GetParameter(P_CONTENT).Value;
            var path = parameters.GetParameterValueOrDefault(P_PATH, "/");
            var isSystemStr = parameters.GetParameterValueOrDefault(P_IS_SYSTEM, "false");
            bool isSystem = isSystemStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            var repositoryId = parameters.GetParameterValueOrDefault(P_REPOSITORY_ID, null);
            var folderId = parameters.GetParameterValueOrDefault(P_FOLDER_ID, null);

            Task<ToolResult> task = SaveFile(toolContext, fileName, content, path, isSystem, repositoryId, folderId, cancellation);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Saving to Drive: {fileName}"
            };
        }

        private async Task<ToolResult> SaveFile(IToolContext toolContext, string fileName, string content,
            string path, bool isSystem, string? repositoryId, string? folderId, CancellationToken cancellation)
        {
            try
            {
                var client = GetDriveClient(toolContext, out var error);
                if (client is null) return error!;

                var contentBytes = Encoding.UTF8.GetBytes(content);
                using var stream = new MemoryStream(contentBytes);

                var contentType = fileName.EndsWith(".md") ? "text/markdown"
                    : fileName.EndsWith(".json") || fileName.EndsWith(".jsonl") ? "application/json"
                    : fileName.EndsWith(".html") ? "text/html"
                    : "text/plain";

                var response = await client.UploadFileAsync(stream, fileName, repositoryId, folderId, path, contentType, isSystem, cancellation);

                if (!response.Success)
                {
                    return new ToolResult()
                    {
                        Success = false,
                        ErrorMessage = response.Error ?? "Upload failed."
                    };
                }

                var result = new
                {
                    response.File?.Id,
                    response.File?.Name,
                    response.File?.Path,
                    SizeBytes = contentBytes.Length,
                    IsSystem = isSystem,
                    RepositoryId = repositoryId,
                    FolderId = folderId
                };

                return new ToolResult()
                {
                    Success = true,
                    Output = JsonSerializer.Serialize(result),
                    OutputFormat = InferenceOutputFormats.Json,
                    OutputMessage = $"Saved {fileName} to Drive ({contentBytes.Length:N0} bytes)"
                };
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
