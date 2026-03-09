using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Coding
{
    public abstract class InferenceToolBase : DaisiToolBase
    {
        protected static async Task<ToolResult> RunInference(IToolContext toolContext, string prompt, string outputMessage)
        {
            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = prompt;

            var infResult = await toolContext.InferAsync(infRequest);

            return new ToolResult
            {
                Output = infResult.Content,
                OutputMessage = outputMessage,
                OutputFormat = InferenceOutputFormats.Markdown,
                Success = true
            };
        }
    }
}
