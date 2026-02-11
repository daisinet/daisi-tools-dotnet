using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Coding
{
    public class GenerateCodeTool : DaisiToolBase
    {
        private const string P_DESCRIPTION = "description";
        private const string P_LANGUAGE = "language";
        private const string P_CONTEXT = "context";

        public override string Id => "daisi-code-generate";
        public override string Name => "Daisi Generate Code";

        public override string UseInstructions =>
            "Use this tool to write and generate programming source code in languages like Python, C#, JavaScript, Java, etc. " +
            "Takes a natural language description and produces working code. " +
            "Keywords: generate code, write code, create function, write program, programming, code generation. " +
            "Do NOT use for explaining existing code or for image prompts.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_DESCRIPTION, Description = "A description of what the code should do.", IsRequired = true },
            new ToolParameter() { Name = P_LANGUAGE, Description = "The programming language to generate code in (e.g. \"Python\", \"C#\", \"JavaScript\").", IsRequired = true },
            new ToolParameter() { Name = P_CONTEXT, Description = "Optional additional context or constraints for the code generation.", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var description = parameters.GetParameterValueOrDefault(P_DESCRIPTION);
            var language = parameters.GetParameterValueOrDefault(P_LANGUAGE);
            var context = parameters.GetParameter(P_CONTEXT, false)?.Value;

            return new ToolExecutionContext
            {
                ExecutionMessage = $"Generating {language} code",
                ExecutionTask = GenerateCode(toolContext, description, language, context)
            };
        }

        private async Task<ToolResult> GenerateCode(IToolContext toolContext, string description, string language, string? context)
        {
            var contextClause = string.IsNullOrWhiteSpace(context)
                ? string.Empty
                : $"\nAdditional context: {context}\n";

            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = $"Generate {language} code for the following:\n\n" +
                $"Description: {description}\n" +
                contextClause +
                $"\nProvide clean, well-structured {language} code. " +
                "Include brief comments explaining key parts. " +
                $"Format the output as a markdown code block with the language tag ```{language.ToLowerInvariant()}\n";

            var infResult = await toolContext.InferAsync(infRequest);

            return new ToolResult
            {
                Output = infResult.Content,
                OutputMessage = $"Generated {language} code",
                OutputFormat = InferenceOutputFormats.Markdown,
                Success = true
            };
        }
    }
}
