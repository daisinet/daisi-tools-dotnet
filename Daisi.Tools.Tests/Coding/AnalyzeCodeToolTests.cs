using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Coding;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Coding
{
    public class AnalyzeCodeToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new AnalyzeCodeTool();
            Assert.Equal("daisi-code-analyze", tool.Id);
        }

        [Fact]
        public void Parameters_CodeIsRequired()
        {
            var tool = new AnalyzeCodeTool();
            Assert.True(tool.Parameters.First(p => p.Name == "code").IsRequired);
        }

        [Fact]
        public void Parameters_LanguageIsOptional()
        {
            var tool = new AnalyzeCodeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "language").IsRequired);
        }

        [Fact]
        public void Parameters_FocusIsOptional()
        {
            var tool = new AnalyzeCodeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "focus").IsRequired);
        }

        [Fact]
        public void GetFocusInstructions_BugsReturnsBugFocus()
        {
            var instructions = AnalyzeCodeTool.GetFocusInstructions("bugs");
            Assert.Contains("logic errors", instructions);
            Assert.Contains("null reference", instructions);
        }

        [Fact]
        public void GetFocusInstructions_SecurityReturnsSecurityFocus()
        {
            var instructions = AnalyzeCodeTool.GetFocusInstructions("security");
            Assert.Contains("security vulnerabilities", instructions);
            Assert.Contains("injection", instructions);
        }

        [Fact]
        public void GetFocusInstructions_StyleReturnsStyleFocus()
        {
            var instructions = AnalyzeCodeTool.GetFocusInstructions("style");
            Assert.Contains("naming conventions", instructions);
            Assert.Contains("readability", instructions);
        }

        [Fact]
        public void GetFocusInstructions_PerformanceReturnsPerformanceFocus()
        {
            var instructions = AnalyzeCodeTool.GetFocusInstructions("performance");
            Assert.Contains("performance", instructions);
            Assert.Contains("allocations", instructions);
        }

        [Fact]
        public void GetFocusInstructions_AllReturnsComprehensive()
        {
            var instructions = AnalyzeCodeTool.GetFocusInstructions("all");
            Assert.Contains("comprehensive", instructions);
        }

        [Fact]
        public async Task Execute_SendsInferenceWithCode()
        {
            var tool = new AnalyzeCodeTool();
            var context = new MockToolContext(req =>
                Task.FromResult(new SendInferenceResponse { Content = "## Summary\nNo issues found." }));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "code", Value = "Console.WriteLine(\"Hello\");", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Markdown, result.OutputFormat);
            Assert.Single(context.InferRequests);
            Assert.Contains("Console.WriteLine", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_WithLanguage_IncludesLanguageInPrompt()
        {
            var tool = new AnalyzeCodeTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "code", Value = "print('hello')", IsRequired = true },
                new() { Name = "language", Value = "Python", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("Python", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_WithoutLanguage_AutoDetects()
        {
            var tool = new AnalyzeCodeTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "code", Value = "var x = 1;", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("Auto-detect", context.InferRequests[0].Text);
        }

        [Fact]
        public async Task Execute_WithFocus_IncludesFocusInPrompt()
        {
            var tool = new AnalyzeCodeTool();
            var context = new MockToolContext();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "code", Value = "var x = 1;", IsRequired = true },
                new() { Name = "focus", Value = "security", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.Contains("security vulnerabilities", context.InferRequests[0].Text);
        }
    }
}
