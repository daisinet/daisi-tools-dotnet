using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Strings
{
    public class UrlEncodeTool : DaisiToolBase
    {
        private const string P_TEXT = "text";
        private const string P_MODE = "mode";

        public override string Id => "daisi-strings-url-encode";
        public override string Name => "Daisi URL Encode";

        public override string UseInstructions =>
            "Use this tool ONLY for URL encoding (percent-encoding) or URL decoding. " +
            "URL encoding replaces special characters like spaces, ampersands, and equals signs with percent codes like %20, %26, %3D for safe use in URLs. " +
            "Keywords: url encode, percent encode, url decode, query string, %20. " +
            "Do NOT use for Base64 encoding.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_TEXT, Description = "The text to encode or decode.", IsRequired = true },
            new ToolParameter() { Name = P_MODE, Description = "The mode: \"encode\" or \"decode\". Default is \"encode\".", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var text = parameters.GetParameterValueOrDefault(P_TEXT);
            var mode = parameters.GetParameterValueOrDefault(P_MODE, "encode");

            return new ToolExecutionContext
            {
                ExecutionMessage = $"URL {mode} text",
                ExecutionTask = Task.Run(() => Execute(text, mode))
            };
        }

        internal static ToolResult Execute(string text, string mode)
        {
            try
            {
                var result = mode?.ToLowerInvariant() switch
                {
                    "decode" => Uri.UnescapeDataString(text),
                    _ => Uri.EscapeDataString(text)
                };

                return new ToolResult
                {
                    Output = result,
                    OutputMessage = $"URL {mode} result",
                    OutputFormat = Protos.V1.InferenceOutputFormats.PlainText,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ToolResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
