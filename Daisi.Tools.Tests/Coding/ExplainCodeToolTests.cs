using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Coding;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Coding
{
    public class ExplainCodeToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new ExplainCodeTool();
            Assert.Equal("daisi-code-explain", tool.Id);
        }

        [Fact]
        public void Parameters_CodeIsRequired()
        {
            var tool = new ExplainCodeTool();
            Assert.True(tool.Parameters.First(p => p.Name == "code").IsRequired);
        }

        [Fact]
        public void Parameters_LanguageIsOptional()
        {
            var tool = new ExplainCodeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "language").IsRequired);
        }

        [Fact]
        public void Parameters_LevelIsOptional()
        {
            var tool = new ExplainCodeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "level").IsRequired);
        }

        [Fact]
        public void GetLevelInstructions_BeginnerLevel()
        {
            var instructions = ExplainCodeTool.GetLevelInstructions("beginner");
            Assert.Contains("beginner", instructions);
            Assert.Contains("simple language", instructions);
        }

        [Fact]
        public void GetLevelInstructions_ExpertLevel()
        {
            var instructions = ExplainCodeTool.GetLevelInstructions("expert");
            Assert.Contains("expert", instructions);
            Assert.Contains("design patterns", instructions);
        }

        [Fact]
        public async Task Execute_SendsInferenceWithCode()
        {
            var tool = new ExplainCodeTool();
            var context = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "## Overview\nThis code prints hello." }));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "code", Value = "print('hello')", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Markdown, result.OutputFormat);
            Assert.Single(context.InferRequests);
            Assert.Contains("print('hello')", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_WithLanguage_IncludesLanguageInPrompt()
        {
            var tool = new ExplainCodeTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "code", Value = "var x = 1;", IsRequired = true },
                new() { Name = "language", Value = "JavaScript", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("JavaScript", context.InferRequests[0].Text);
        }
    }
}
