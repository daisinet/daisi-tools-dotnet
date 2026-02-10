using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Information
{
    public class TranslateTextTool : DaisiToolBase
    {
        private const string P_TEXT = "text";
        private const string P_TARGET_LANGUAGE = "target-language";
        private const string P_SOURCE_LANGUAGE = "source-language";

        public override string Id => "daisi-info-translate";
        public override string Name => "Daisi Translate Text";

        public override string UseInstructions =>
            "Use this tool to translate text from one language to another. " +
            "Provide the text and target language. Source language is auto-detected if not specified.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_TEXT,
                Description = "The text to translate.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_TARGET_LANGUAGE,
                Description = "The language to translate the text into (e.g. \"Spanish\", \"French\", \"Japanese\").",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_SOURCE_LANGUAGE,
                Description = "The language of the source text. If not provided, the language will be auto-detected.",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pText = parameters.GetParameter(P_TEXT);
            var text = pText.Value;

            var pTargetLang = parameters.GetParameter(P_TARGET_LANGUAGE);
            var targetLanguage = pTargetLang.Value;

            var pSourceLang = parameters.GetParameter(P_SOURCE_LANGUAGE, false);
            var sourceLanguage = pSourceLang?.Value;

            Task<ToolResult> task = TranslateText(toolContext, text, targetLanguage, sourceLanguage);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Translating text to {targetLanguage}."
            };
        }

        private async Task<ToolResult> TranslateText(IToolContext toolContext, string text, string targetLanguage, string? sourceLanguage)
        {
            var result = new ToolResult();
            result.OutputFormat = InferenceOutputFormats.PlainText;

            var sourceClause = string.IsNullOrWhiteSpace(sourceLanguage)
                ? "Auto-detect the source language."
                : $"The source language is {sourceLanguage}.";

            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = $"Text:\n{text}\n\n" +
                $"Translate the above text into {targetLanguage}. {sourceClause} " +
                "Preserve the original tone, style, and meaning as closely as possible. " +
                "Only output the translated text, nothing else.";

            var infResult = await toolContext.InferAsync(infRequest);

            result.Output = infResult.Content;
            result.OutputMessage = $"Text translated to {targetLanguage}";
            result.Success = true;

            return result;
        }
    }
}
