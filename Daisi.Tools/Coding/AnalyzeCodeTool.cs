using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Coding
{
    public class AnalyzeCodeTool : InferenceToolBase
    {
        private const string P_CODE = "code";
        private const string P_LANGUAGE = "language";
        private const string P_FOCUS = "focus";

        public override string Id => "daisi-code-analyze";
        public override string Name => "Daisi Analyze Code";

        public override string UseInstructions =>
            "Use this tool to analyze source code for bugs, security issues, style problems, or performance concerns. " +
            "Provide the code snippet and get a detailed review with severity ratings. " +
            "Keywords: analyze code, code review, find bugs, security audit, code quality. " +
            "Do NOT use for explaining code — use daisi-code-explain for explanations.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_CODE,
                Description = "The source code to analyze.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_LANGUAGE,
                Description = "The programming language of the code (e.g. \"C#\", \"Python\", \"JavaScript\"). Auto-detected if not specified.",
                IsRequired = false
            },
            new ToolParameter(){
                Name = P_FOCUS,
                Description = "The focus area for analysis. Options: \"bugs\", \"security\", \"style\", \"performance\", \"all\". Default is \"all\".",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pCode = parameters.GetParameter(P_CODE);
            var code = pCode.Value;

            var pLanguage = parameters.GetParameter(P_LANGUAGE, false);
            var language = pLanguage?.Value;
            var focus = parameters.GetParameterValueOrDefault(P_FOCUS, "all");

            Task<ToolResult> task = AnalyzeCode(toolContext, code, language, focus);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = "Analyzing code."
            };
        }

        private async Task<ToolResult> AnalyzeCode(IToolContext toolContext, string code, string? language, string focus)
        {
            var languageClause = string.IsNullOrWhiteSpace(language)
                ? "Auto-detect the programming language."
                : $"The code is written in {language}.";

            var focusInstructions = GetFocusInstructions(focus);

            var prompt = $"Code:\n```\n{code}\n```\n\n" +
                $"{languageClause}\n\n" +
                $"Analyze the code with the following focus: {focusInstructions}\n\n" +
                "Format your response as markdown with these sections:\n" +
                "## Summary\nA brief overview of the code and overall assessment.\n\n" +
                "## Findings\nFor each issue found, provide:\n" +
                "- **Severity**: Critical/High/Medium/Low\n" +
                "- **Description**: What the issue is\n" +
                "- **Location**: Where in the code it occurs\n" +
                "- **Recommendation**: How to fix it\n\n" +
                "If no issues are found, state that the code looks clean for the given focus area.";

            return await RunInference(toolContext, prompt, $"Code analysis complete (focus: {focus})");
        }

        internal static string GetFocusInstructions(string focus)
        {
            return focus.ToLower() switch
            {
                "bugs" => "Look for logic errors, null reference issues, off-by-one errors, edge cases, and incorrect behavior.",
                "security" => "Look for security vulnerabilities including injection, authentication issues, data exposure, and unsafe operations.",
                "style" => "Look for code style issues including naming conventions, readability, code organization, and best practices.",
                "performance" => "Look for performance issues including unnecessary allocations, inefficient algorithms, N+1 queries, and resource leaks.",
                _ => "Perform a comprehensive analysis covering bugs, security vulnerabilities, code style, and performance issues."
            };
        }
    }
}
