using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text.Json;

namespace Daisi.Tools.Drive
{
    public class FileListTool : DriveToolBase
    {
        private const string P_REPOSITORY_ID = "repository-id";
        private const string P_FOLDER_ID = "folder-id";

        public override string Id => "daisi-drive-list";
        public override string Name => "Daisi Drive List Files";

        public override string UseInstructions =>
            "Use this tool to list files in Daisi Drive. " +
            "Optionally scope to a specific repository and folder. " +
            "Returns file names, IDs, sizes, and types.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_REPOSITORY_ID,
                Description = "Optional repository ID to list files from. Defaults to Account repository.",
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
            var repositoryId = parameters.GetParameterValueOrDefault(P_REPOSITORY_ID, null);
            var folderId = parameters.GetParameterValueOrDefault(P_FOLDER_ID, null);

            Task<ToolResult> task = ListFiles(toolContext, repositoryId, folderId, cancellation);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = "Listing Drive files"
            };
        }

        private async Task<ToolResult> ListFiles(IToolContext toolContext, string? repositoryId, string? folderId, CancellationToken cancellation)
        {
            try
            {
                var client = GetDriveClient(toolContext, out var error);
                if (client is null) return error!;

                var response = await client.ListFilesAsync(repositoryId, folderId, cancellationToken: cancellation);

                var files = response.Files.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Path,
                    f.SizeBytes,
                    f.ContentType,
                    f.RepositoryId,
                    f.FolderId
                }).ToList();

                var folders = response.SubFolders.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.FullPath,
                    f.RepositoryId
                }).ToList();

                var result = new { Files = files, Folders = folders };

                return new ToolResult()
                {
                    Success = true,
                    Output = JsonSerializer.Serialize(result),
                    OutputFormat = InferenceOutputFormats.Json,
                    OutputMessage = $"Found {files.Count} files and {folders.Count} folders"
                };
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
