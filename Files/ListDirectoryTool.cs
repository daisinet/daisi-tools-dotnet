using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text.Json;

namespace Daisi.Tools.Files
{
    public class ListDirectoryTool : DaisiToolBase
    {
        private const string P_PATH = "path";
        private const string P_PATTERN = "pattern";

        public override string Id => "daisi-files-list-directory";
        public override string Name => "Daisi List Directory";

        public override string UseInstructions =>
            "Use this tool to list files and subdirectories in a directory. " +
            "Optionally provide a glob pattern to filter results.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_PATH,
                Description = "The full path to the directory to list.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_PATTERN,
                Description = "A glob pattern to filter results (e.g. \"*.txt\"). Default is \"*\".",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pPath = parameters.GetParameter(P_PATH);
            var path = pPath.Value;

            var pattern = parameters.GetParameterValueOrDefault(P_PATTERN, "*");

            Task<ToolResult> task = ListDirectory(path, pattern);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Listing directory: {path}"
            };
        }

        private async Task<ToolResult> ListDirectory(string path, string pattern)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return new ToolResult()
                    {
                        Success = false,
                        ErrorMessage = $"Directory not found: {path}"
                    };
                }

                var entries = new List<object>();

                foreach (var dir in Directory.GetDirectories(path, pattern))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    entries.Add(new
                    {
                        name = dirInfo.Name,
                        type = "directory",
                        modified = dirInfo.LastWriteTimeUtc.ToString("o")
                    });
                }

                foreach (var file in Directory.GetFiles(path, pattern))
                {
                    var fileInfo = new FileInfo(file);
                    entries.Add(new
                    {
                        name = fileInfo.Name,
                        type = "file",
                        size = fileInfo.Length,
                        modified = fileInfo.LastWriteTimeUtc.ToString("o")
                    });
                }

                var result = new ToolResult();
                result.OutputFormat = InferenceOutputFormats.Json;
                result.Output = JsonSerializer.Serialize(entries);
                result.OutputMessage = $"Found {entries.Count} entries in {path}";
                result.Success = true;

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
