using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Information
{
    public class SummarizeTextTool : DaisiToolBase
    {
        private const string P_TEXT = "text";
        private const string P_MAX_LENGTH = "max-length";

        public override string Id => "daisi-info-summarize-text";
        public override string Name => "Daisi Summarize Text";

        public override string UseInstructions =>
            "Use this tool to summarize plain text (NOT HTML). " +
            "Takes a plain text paragraph, article, or passage and produces a concise summary. " +
            "Keywords: summarize text, shorten, condense, brief summary, tldr. " +
            "Do NOT use for HTML content — use daisi-web-html-summarize for HTML.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_TEXT,
                Description = "The text content to summarize.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_MAX_LENGTH,
                Description = "The maximum length for the summary. Default is \"500 words\".",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pText = parameters.GetParameter(P_TEXT);
            var text = pText.Value;

            var maxLength = parameters.GetParameterValueOrDefault(P_MAX_LENGTH, "500 words");

            Task<ToolResult> task = SummarizeText(toolContext, text, maxLength);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = "Summarizing text content."
            };
        }

        private async Task<ToolResult> SummarizeText(IToolContext toolContext, string text, string maxLength)
        {
            var result = new ToolResult();
            result.OutputFormat = InferenceOutputFormats.PlainText;

            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = $"Text:\n{text}\n\nSummarize the above text in under {maxLength}. " +
                "Focus on the most important points and key information. " +
                "Produce a clear, concise summary that captures the essential meaning.";

            var infResult = await toolContext.InferAsync(infRequest);

            result.Output = infResult.Content;
            result.OutputMessage = "Summary of the provided text";
            result.Success = true;

            return result;
        }
    }
}
