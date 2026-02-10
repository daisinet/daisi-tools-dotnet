using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Information
{
    public class TranslateTextToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new TranslateTextTool();
            Assert.Equal("daisi-info-translate", tool.Id);
        }

        [Fact]
        public void Parameters_TextAndTargetLanguageAreRequired()
        {
            var tool = new TranslateTextTool();
            Assert.True(tool.Parameters.First(p => p.Name == "text").IsRequired);
            Assert.True(tool.Parameters.First(p => p.Name == "target-language").IsRequired);
        }

        [Fact]
        public void Parameters_SourceLanguageIsOptional()
        {
            var tool = new TranslateTextTool();
            Assert.False(tool.Parameters.First(p => p.Name == "source-language").IsRequired);
        }

        [Fact]
        public async Task Execute_AutoDetectsSourceLanguage()
        {
            var tool = new TranslateTextTool();
            var context = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "Hola mundo" }));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "Hello world", IsRequired = true },
                new() { Name = "target-language", Value = "Spanish", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("Hola mundo", result.Output);
            Assert.Contains("Auto-detect", context.InferRequests[0].Text);
            Assert.Contains("Spanish", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_UsesExplicitSourceLanguage()
        {
            var tool = new TranslateTextTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "Hello world", IsRequired = true },
                new() { Name = "target-language", Value = "French", IsRequired = true },
                new() { Name = "source-language", Value = "English", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("The source language is English", context.InferRequests[0].Text);
            Assert.DoesNotContain("Auto-detect", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_OutputFormatIsPlainText()
        {
            var tool = new TranslateTextTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "Hello", IsRequired = true },
                new() { Name = "target-language", Value = "German", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.Equal(InferenceOutputFormats.PlainText, result.OutputFormat);
        }

        [Fact]
        public void Validate_MissingTargetLanguageReturnsError()
        {
            var tool = new TranslateTextTool();
            var param = new ToolParameterBase { Name = "target-language", Value = "", IsRequired = true };
            var error = tool.ValidateGeneratedParameterValues(param);
            Assert.NotNull(error);
        }
    }
}
