using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text;

namespace Daisi.Tools.Strings
{
    public class Base64Tool : DaisiToolBase
    {
        private const string P_TEXT = "text";
        private const string P_MODE = "mode";

        public override string Id => "daisi-strings-base64";
        public override string Name => "Daisi Base64";

        public override string UseInstructions =>
            "Use this tool ONLY for Base64 encoding or decoding. " +
            "Base64 converts text into a string of letters, digits, +, and / characters. " +
            "Keywords: base64, b64. " +
            "Do NOT use for URL encoding or any other string operation.";

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
                ExecutionMessage = $"Base64 {mode} text",
                ExecutionTask = Task.Run(() => Execute(text, mode))
            };
        }

        internal static ToolResult Execute(string text, string mode)
        {
            try
            {
                var result = mode?.ToLowerInvariant() switch
                {
                    "decode" => Encoding.UTF8.GetString(Convert.FromBase64String(text)),
                    _ => Convert.ToBase64String(Encoding.UTF8.GetBytes(text))
                };

                return new ToolResult
                {
                    Output = result,
                    OutputMessage = $"Base64 {mode} result",
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
