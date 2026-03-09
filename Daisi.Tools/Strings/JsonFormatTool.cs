using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Text.Json;

namespace Daisi.Tools.Strings
{
    public class JsonFormatTool : DaisiToolBase
    {
        private const string P_JSON = "json";
        private const string P_ACTION = "action";

        public override string Id => "daisi-strings-json-format";
        public override string Name => "Daisi JSON Format";

        public override string UseInstructions =>
            "Use this tool to pretty-print, minify, or validate JSON data. " +
            "Reformats JSON text for readability or compactness. " +
            "Keywords: json, format json, pretty print, minify, validate json, json format.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_JSON, Description = "The JSON text to process.", IsRequired = true },
            new ToolParameter() { Name = P_ACTION, Description = "The action: \"pretty\", \"minify\", or \"validate\". Default is \"pretty\".", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var json = parameters.GetParameterValueOrDefault(P_JSON);
            var action = parameters.GetParameterValueOrDefault(P_ACTION, "pretty");

            return new ToolExecutionContext
            {
                ExecutionMessage = $"JSON {action}",
                ExecutionTask = Task.Run(() => Execute(json, action))
            };
        }

        internal static ToolResult Execute(string json, string action)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                var result = action?.ToLowerInvariant() switch
                {
                    "minify" => JsonSerializer.Serialize(doc.RootElement),
                    "validate" => "Valid JSON",
                    _ => JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true })
                };

                return new ToolResult
                {
                    Output = result,
                    OutputMessage = $"JSON {action} result",
                    OutputFormat = action?.ToLowerInvariant() == "validate"
                        ? Protos.V1.InferenceOutputFormats.PlainText
                        : Protos.V1.InferenceOutputFormats.Json,
                    Success = true
                };
            }
            catch (JsonException ex)
            {
                if (action?.ToLowerInvariant() == "validate")
                {
                    return new ToolResult
                    {
                        Output = $"Invalid JSON: {ex.Message}",
                        OutputMessage = "JSON validation failed",
                        OutputFormat = Protos.V1.InferenceOutputFormats.PlainText,
                        Success = true
                    };
                }

                return new ToolResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
