using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Daisi.Tools.Web.Html
{
    public class SummarizeHtmlTool : DaisiToolBase
    {
        private const string P_HTML = "html";

        public override string Name => "Daisi Summarize HTML";

        public override string Description => "Do NOT use this tool to get HTML from the web. To get HTML from the web use the Daisi Web Client first, then use this tool with the HTML provided. Use this tool to produce a summary of HTML code that is already available. ";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){ Name = P_HTML, Description = "Do NOT provide a URL for a website. This is ONLY the properly formatted HTML code that needs to be summarized for human readability.", IsRequired = true }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pHtml = parameters.GetParameter(P_HTML);
            var html = pHtml.Values.FirstOrDefault();

            Task<ToolResult> task = SummarizeHtml(toolContext, html);

            ToolExecutionContext toolExecutionContext = new()
            {
                ExecutionTask = task,
                ExecutionMessage = "Summarizing the HTML for humans."
            };

            return toolExecutionContext;

        }

        private async Task<ToolResult> SummarizeHtml(IToolContext toolContext, string html) {
            ToolResult result = new ToolResult();
            result.OutputFormat = Protos.V1.InferenceOutputFormats.PlainText;

            var summary = string.Empty;
            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = $"Summarize the following HTML so that a human can read it:\n{html}";
            var infResult = await toolContext.InferAsync(infRequest);
            result.Output = infResult.Content;
            result.OutputMessage = $"This is a summary of the HTML";
            return result;
        }
    }
}
