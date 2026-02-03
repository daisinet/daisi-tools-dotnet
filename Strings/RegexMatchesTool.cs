using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Daisi.Tools.Strings
{
    public class RegexMatchesTool : DaisiToolBase
    {
        public override string Id => "daisi-strings-regex-matching";

        public override string Name => "Daisi Regex Matching Tool";

        public override string UseInstructions => "Use this tool to extract strings from a source text.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){ Name = "input", Description="This is the source text from which matches will be searched and extracted.", IsRequired = true },
            new ToolParameter(){ Name = "pattern", Description="This is the regex pattern that will be used to find matches in the source text.", IsRequired = true }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            string input = parameters.GetParameterValueOrDefault("input");
            string pattern = parameters.GetParameterValueOrDefault("pattern");

            ToolExecutionContext toolExecutionContext = new()
            {
                ExecutionMessage = "Finding Regex Matches",
                ExecutionTask = RunMatching(input, pattern)
            };

            return toolExecutionContext;
        }
        async Task<ToolResult> RunMatching(string? input, string? pattern)
        {
            ToolResult toolResult = new ToolResult();
            var matches = Regex.Matches(input, pattern);

            toolResult.Output = JsonSerializer.Serialize(matches.Select(m => m.Value).ToArray());
            toolResult.OutputMessage = "Extracted Values in JSON Format";
            toolResult.OutputFormat = Protos.V1.InferenceOutputFormats.Json;

            return toolResult;
        }
    }
}
