using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text;

namespace Daisi.Tools.Drive
{
    public class FileReadTool : DriveToolBase
    {
        private const string P_FILE_ID = "file-id";
        private const int MaxContentLength = 50_000;

        public override string Id => "daisi-drive-read";
        public override string Name => "Daisi Drive Read File";

        public override string UseInstructions =>
            "Use this tool to read the contents of a file stored in Daisi Drive. " +
            "Requires the file ID (obtained from search results or file listings). " +
            "Returns the text content of the file, truncated if very large.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_FILE_ID,
                Description = "The ID of the file to read from Drive.",
                IsRequired = true
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var fileId = parameters.GetParameter(P_FILE_ID).Value;

            Task<ToolResult> task = ReadFile(toolContext, fileId, cancellation);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Reading Drive file: {fileId}"
            };
        }

        private async Task<ToolResult> ReadFile(IToolContext toolContext, string fileId, CancellationToken cancellation)
        {
            try
            {
                var client = GetDriveClient(toolContext, out var error);
                if (client is null) return error!;

                var data = await client.DownloadFileAsync(fileId, cancellation);

                var content = Encoding.UTF8.GetString(data);
                bool truncated = false;

                if (content.Length > MaxContentLength)
                {
                    content = content[..MaxContentLength];
                    truncated = true;
                }

                return new ToolResult()
                {
                    Success = true,
                    Output = content,
                    OutputFormat = InferenceOutputFormats.PlainText,
                    OutputMessage = truncated
                        ? $"File content (truncated from {data.Length:N0} bytes to {MaxContentLength:N0} chars)"
                        : $"File content ({data.Length:N0} bytes)"
                };
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
