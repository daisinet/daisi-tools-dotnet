using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;

namespace Daisi.Tools.Media
{
    public class ImagePromptTool : DaisiToolBase
    {
        private const string P_DESCRIPTION = "description";
        private const string P_STYLE = "style";

        public override string Id => "daisi-media-image-prompt";
        public override string Name => "Daisi Image Prompt";

        public override string UseInstructions =>
            "Use this tool to create detailed text prompts for AI image generators like Stable Diffusion, DALL-E, or Midjourney. " +
            "Converts a simple scene description into an optimized image generation prompt with composition, lighting, and style details. " +
            "Keywords: image prompt, stable diffusion, dall-e, midjourney, image generation, picture, photo prompt, visual art. " +
            "Do NOT use for code generation — use daisi-code-generate for programming code.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_DESCRIPTION, Description = "A simple description of the desired image.", IsRequired = true },
            new ToolParameter() { Name = P_STYLE, Description = "The visual style: \"photorealistic\", \"illustration\", \"sketch\", or \"abstract\". Default is \"photorealistic\".", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var description = parameters.GetParameterValueOrDefault(P_DESCRIPTION);
            var style = parameters.GetParameterValueOrDefault(P_STYLE, "photorealistic");

            return new ToolExecutionContext
            {
                ExecutionMessage = "Generating image prompt",
                ExecutionTask = GeneratePrompt(toolContext, description, style)
            };
        }

        private async Task<ToolResult> GeneratePrompt(IToolContext toolContext, string description, string style)
        {
            var styleInstructions = GetStyleInstructions(style);

            var infRequest = SendInferenceRequest.CreateDefault();
            infRequest.Text = $"Create a detailed image generation prompt based on this description: {description}\n\n" +
                $"Target style: {styleInstructions}\n\n" +
                "Generate a single detailed prompt that includes:\n" +
                "- Subject description with specific details\n" +
                "- Composition and framing\n" +
                "- Lighting conditions\n" +
                "- Color palette\n" +
                "- Mood/atmosphere\n" +
                "- Quality modifiers (e.g., 'highly detailed', '8k', 'professional')\n\n" +
                "Output ONLY the prompt text, no explanations or formatting.";

            var infResult = await toolContext.InferAsync(infRequest);

            return new ToolResult
            {
                Output = infResult.Content,
                OutputMessage = $"Generated {style} image prompt",
                OutputFormat = InferenceOutputFormats.PlainText,
                Success = true
            };
        }

        internal static string GetStyleInstructions(string style)
        {
            return style?.ToLowerInvariant() switch
            {
                "illustration" => "Digital illustration style. Include terms like 'digital art', 'illustration', 'vibrant colors', 'clean lines'.",
                "sketch" => "Hand-drawn sketch style. Include terms like 'pencil sketch', 'line drawing', 'hand-drawn', 'charcoal'.",
                "abstract" => "Abstract art style. Include terms like 'abstract', 'geometric shapes', 'color fields', 'non-representational'.",
                _ => "Photorealistic style. Include terms like 'photograph', 'realistic', 'DSLR', 'natural lighting', 'sharp focus'."
            };
        }
    }
}
