using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Strings;
using Daisi.Tools.Tests.Helpers;
using System.Text.Json;

namespace Daisi.Tools.Tests.Strings
{
    public class RegexMatchesToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new RegexMatchesTool();
            Assert.Equal("daisi-strings-regex-matching", tool.Id);
        }

        [Fact]
        public void Parameters_InputIsRequired()
        {
            var tool = new RegexMatchesTool();
            Assert.True(tool.Parameters.First(p => p.Name == "input").IsRequired);
        }

        [Fact]
        public void Parameters_PatternIsRequired()
        {
            var tool = new RegexMatchesTool();
            Assert.True(tool.Parameters.First(p => p.Name == "pattern").IsRequired);
        }

        [Fact]
        public async Task Execute_FindsEmailAddresses()
        {
            var tool = new RegexMatchesTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "input", Value = "Contact support@example.com or sales@company.org", IsRequired = true },
                new() { Name = "pattern", Value = @"\S+@\S+\.\S+", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            var matches = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(matches);
            Assert.Equal(2, matches!.Length);
            Assert.Contains("support@example.com", matches);
            Assert.Contains("sales@company.org", matches);
        }

        [Fact]
        public async Task Execute_NoMatches_ReturnsEmptyArray()
        {
            var tool = new RegexMatchesTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "input", Value = "no numbers here", IsRequired = true },
                new() { Name = "pattern", Value = @"\d+", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            var matches = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(matches);
            Assert.Empty(matches!);
        }

        [Fact]
        public async Task Execute_ReturnsJsonFormat()
        {
            var tool = new RegexMatchesTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "input", Value = "abc 123 def 456", IsRequired = true },
                new() { Name = "pattern", Value = @"\d+", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);
        }
    }
}
