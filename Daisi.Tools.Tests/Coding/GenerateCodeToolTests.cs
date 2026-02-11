using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Coding;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Coding
{
    public class GenerateCodeToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new GenerateCodeTool();
            Assert.Equal("daisi-code-generate", tool.Id);
        }

        [Fact]
        public void Parameters_DescriptionIsRequired()
        {
            var tool = new GenerateCodeTool();
            Assert.True(tool.Parameters.First(p => p.Name == "description").IsRequired);
        }

        [Fact]
        public void Parameters_LanguageIsRequired()
        {
            var tool = new GenerateCodeTool();
            Assert.True(tool.Parameters.First(p => p.Name == "language").IsRequired);
        }

        [Fact]
        public void Parameters_ContextIsOptional()
        {
            var tool = new GenerateCodeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "context").IsRequired);
        }

        [Fact]
        public async Task Execute_SendsInferenceWithDescription()
        {
            var tool = new GenerateCodeTool();
            var context = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "```python\ndef hello():\n    print('hello')\n```" }));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "description", Value = "A function that prints hello", IsRequired = true },
                new() { Name = "language", Value = "Python", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Markdown, result.OutputFormat);
            Assert.Single(context.InferRequests);
            Assert.Contains("Python", context.InferRequests[0].Text);
            Assert.Contains("function that prints hello", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_WithContext_IncludesContextInPrompt()
        {
            var tool = new GenerateCodeTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "description", Value = "Sort function", IsRequired = true },
                new() { Name = "language", Value = "JavaScript", IsRequired = true },
                new() { Name = "context", Value = "Use arrow function syntax", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("arrow function syntax", context.InferRequests[0].Text);
        }
    }
}
