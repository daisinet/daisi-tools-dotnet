using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Information
{
    public class SummarizeTextToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new SummarizeTextTool();
            Assert.Equal("daisi-info-summarize-text", tool.Id);
        }

        [Fact]
        public void Parameters_TextIsRequired()
        {
            var tool = new SummarizeTextTool();
            var textParam = tool.Parameters.First(p => p.Name == "text");
            Assert.True(textParam.IsRequired);
        }

        [Fact]
        public void Parameters_MaxLengthIsOptional()
        {
            var tool = new SummarizeTextTool();
            var maxLengthParam = tool.Parameters.First(p => p.Name == "max-length");
            Assert.False(maxLengthParam.IsRequired);
        }

        [Fact]
        public async Task Execute_SendsInferenceWithTextContent()
        {
            var tool = new SummarizeTextTool();
            var context = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "Summary result" }));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "This is a long text to summarize.", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("Summary result", result.Output);
            Assert.Equal(InferenceOutputFormats.PlainText, result.OutputFormat);
            Assert.Single(context.InferRequests);
            Assert.Contains("This is a long text to summarize.", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_UsesDefaultMaxLength()
        {
            var tool = new SummarizeTextTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "Some text.", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("500 words", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_UsesCustomMaxLength()
        {
            var tool = new SummarizeTextTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "Some text.", IsRequired = true },
                new() { Name = "max-length", Value = "100 words", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("100 words", context.InferRequests[0].Text);
        }

        [Fact]
        public void Validate_MissingTextReturnsError()
        {
            var tool = new SummarizeTextTool();
            var param = new ToolParameterBase { Name = "text", Value = "", IsRequired = true };
            var error = tool.ValidateGeneratedParameterValues(param);
            Assert.NotNull(error);
        }
    }
}
