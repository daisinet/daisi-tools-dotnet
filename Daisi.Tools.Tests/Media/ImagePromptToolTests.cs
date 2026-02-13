using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Media;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Media
{
    public class ImagePromptToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new ImagePromptTool();
            Assert.Equal("daisi-media-image-prompt", tool.Id);
        }

        [Fact]
        public void Parameters_DescriptionIsRequired()
        {
            var tool = new ImagePromptTool();
            Assert.True(tool.Parameters.First(p => p.Name == "description").IsRequired);
        }

        [Fact]
        public void Parameters_StyleIsOptional()
        {
            var tool = new ImagePromptTool();
            Assert.False(tool.Parameters.First(p => p.Name == "style").IsRequired);
        }

        [Fact]
        public void GetStyleInstructions_Photorealistic()
        {
            var instructions = ImagePromptTool.GetStyleInstructions("photorealistic");
            Assert.Contains("Photorealistic", instructions);
            Assert.Contains("photograph", instructions);
        }

        [Fact]
        public void GetStyleInstructions_Illustration()
        {
            var instructions = ImagePromptTool.GetStyleInstructions("illustration");
            Assert.Contains("illustration", instructions);
        }

        [Fact]
        public async Task Execute_SendsInferenceRequest()
        {
            var tool = new ImagePromptTool();
            var context = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "A photorealistic image of a sunset over mountains, golden hour lighting, 8k, professional" }));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "description", Value = "sunset over mountains", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.PlainText, result.OutputFormat);
            Assert.Single(context.InferRequests);
            Assert.Contains("sunset over mountains", context.InferRequests[0].Text);
        }
    }
}
