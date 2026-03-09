using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Coding
{
    public class ExplainCodeTool : InferenceToolBase
    {
        private const string P_CODE = "code";
        private const string P_LANGUAGE = "language";
        private const string P_LEVEL = "level";

        public override string Id => "daisi-code-explain";
        public override string Name => "Daisi Explain Code";

        public override string UseInstructions =>
            "Use this tool to explain what existing source code does in plain human language. " +
            "Takes a code snippet and returns a readable explanation of how it works. " +
            "Keywords: explain code, what does this code do, code explanation, understand code, code walkthrough. " +
            "Do NOT use for generating new code — use daisi-code-generate instead.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_CODE, Description = "The source code to explain.", IsRequired = true },
            new ToolParameter() { Name = P_LANGUAGE, Description = "The programming language (e.g. \"Python\", \"C#\"). Auto-detected if not specified.", IsRequired = false },
            new ToolParameter() { Name = P_LEVEL, Description = "The audience level: \"beginner\", \"intermediate\", or \"expert\". Default is \"intermediate\".", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var code = parameters.GetParameterValueOrDefault(P_CODE);
            var language = parameters.GetParameter(P_LANGUAGE, false)?.Value;
            var level = parameters.GetParameterValueOrDefault(P_LEVEL, "intermediate");

            return new ToolExecutionContext
            {
                ExecutionMessage = "Explaining code",
                ExecutionTask = ExplainCode(toolContext, code, language, level)
            };
        }

        private async Task<ToolResult> ExplainCode(IToolContext toolContext, string code, string? language, string level)
        {
            var languageClause = string.IsNullOrWhiteSpace(language)
                ? "Auto-detect the programming language."
                : $"The code is written in {language}.";

            var levelInstructions = GetLevelInstructions(level);

            var prompt = $"Code:\n```\n{code}\n```\n\n" +
                $"{languageClause}\n\n" +
                $"Explain this code {levelInstructions}\n\n" +
                "Format your response as markdown with:\n" +
                "## Overview\nA brief summary of what the code does.\n\n" +
                "## Step-by-Step Explanation\nWalk through the code explaining each important part.\n\n" +
                "## Key Concepts\nList any important programming concepts used.";

            return await RunInference(toolContext, prompt, $"Code explanation ({level})");
        }

        internal static string GetLevelInstructions(string level)
        {
            return level?.ToLowerInvariant() switch
            {
                "beginner" => "for a beginner programmer. Use simple language, avoid jargon, and explain basic concepts that an experienced developer would take for granted.",
                "expert" => "for an expert programmer. Focus on design patterns, performance implications, edge cases, and architectural decisions. Skip basic explanations.",
                _ => "for an intermediate programmer. Explain the logic and approach, but don't over-explain basic syntax."
            };
        }
    }
}
