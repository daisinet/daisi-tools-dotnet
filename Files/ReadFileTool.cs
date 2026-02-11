using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Files
{
    public class ReadFileTool : DaisiToolBase
    {
        private const string P_PATH = "path";
        private const int MaxFileSizeBytes = 1_048_576; // 1MB

        public override string Id => "daisi-files-read";
        public override string Name => "Daisi Read File";

        public override string UseInstructions =>
            "Use this tool when the user wants to read or view a file's contents from the local filesystem. " +
            "Provide the full file path. Returns the text content of the file. " +
            "Files larger than 1MB will be truncated.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_PATH,
                Description = "The full path to the file to read.",
                IsRequired = true
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pPath = parameters.GetParameter(P_PATH);
            var path = pPath.Value;

            Task<ToolResult> task = ReadFile(path);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Reading file: {path}"
            };
        }

        private async Task<ToolResult> ReadFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new ToolResult()
                    {
                        Success = false,
                        ErrorMessage = $"File not found: {path}"
                    };
                }

                var fileInfo = new FileInfo(path);
                var content = await File.ReadAllTextAsync(path);
                bool truncated = false;

                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    content = content[..MaxFileSizeBytes];
                    truncated = true;
                }

                var result = new ToolResult();
                result.OutputFormat = InferenceOutputFormats.PlainText;
                result.Output = content;
                result.Success = true;

                result.OutputMessage = truncated
                    ? $"File content (truncated from {fileInfo.Length:N0} bytes to {MaxFileSizeBytes:N0} bytes)"
                    : $"File content ({fileInfo.Length:N0} bytes)";

                return result;
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
