using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;


namespace Daisi.Tools.Web.Html
{
    public class SummarizeHtmlTool : DaisiToolBase
    {
        private const string P_HTML = "html";

        public override string Id => "daisi-web-html-summarize";
        public override string Name => "Daisi Summarize HTML";

        public override string UseInstructions =>
            "Use this tool when the user has HTML content and wants a human-readable summary of it. " +
            "Provide the raw HTML code as the parameter value. " +
            "Do NOT use this to fetch HTML from the web — use daisi-web-clients-http-get to fetch HTML first, then pass it here.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_HTML,
                Description = "Do NOT provide a URL for a website. This is ONLY " +
                "the properly formatted HTML code that needs to be summarized for " +
                "human readability.",
                IsRequired = true
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pHtml = parameters.GetParameter(P_HTML);
            var html = pHtml.Value;

            Task<ToolResult> task = SummarizeHtml(toolContext, html);

            ToolExecutionContext toolExecutionContext = new()
            {
                ExecutionTask = task,
                ExecutionMessage = "Summarizing HTML for human consumption."
            };

            return toolExecutionContext;

        }

        private async Task<ToolResult> SummarizeHtml(IToolContext toolContext, string html)
        {
            ToolResult result = new ToolResult();
            result.OutputFormat = Protos.V1.InferenceOutputFormats.PlainText;

            var summary = string.Empty;

            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = $"HTML:\n{html}\n\nFirst, determine the main content area. Ignore navigations, footers, and headers. In under 500 words, summarize the main content area in a way that humans will understand to most important parts.";

            var infResult = await toolContext.InferAsync(infRequest);

            result.Output = infResult.Content;
            result.OutputMessage = $"This is a summary of the HTML";

            return result;
        }
    }
}
