using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text;

namespace Daisi.Tools.Files
{
    public class WriteFileTool : DaisiToolBase
    {
        private const string P_PATH = "path";
        private const string P_CONTENT = "content";

        public override string Id => "daisi-files-write";
        public override string Name => "Daisi Write File";

        public override string UseInstructions =>
            "Use this tool when the user wants to write, save, or create a file on the local filesystem. " +
            "Provide the full file path and the content to write. " +
            "Creates parent directories if needed. Overwrites existing files.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_PATH,
                Description = "The full path to the file to write.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_CONTENT,
                Description = "The content to write to the file.",
                IsRequired = true
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pPath = parameters.GetParameter(P_PATH);
            var path = pPath.Value;

            var pContent = parameters.GetParameter(P_CONTENT);
            var content = pContent.Value;

            Task<ToolResult> task = WriteFile(path, content);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Writing file: {path}"
            };
        }

        private async Task<ToolResult> WriteFile(string path, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(path, content);

                var byteCount = Encoding.UTF8.GetByteCount(content);

                return new ToolResult()
                {
                    Success = true,
                    Output = $"Successfully wrote {byteCount:N0} bytes to {path}",
                    OutputMessage = $"File written: {path}",
                    OutputFormat = InferenceOutputFormats.PlainText
                };
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
