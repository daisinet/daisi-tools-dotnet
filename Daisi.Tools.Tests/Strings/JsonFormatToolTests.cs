using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Strings;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Strings
{
    public class JsonFormatToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new JsonFormatTool();
            Assert.Equal("daisi-strings-json-format", tool.Id);
        }

        [Fact]
        public void Parameters_JsonIsRequired()
        {
            var tool = new JsonFormatTool();
            Assert.True(tool.Parameters.First(p => p.Name == "json").IsRequired);
        }

        [Fact]
        public void Parameters_ActionIsOptional()
        {
            var tool = new JsonFormatTool();
            Assert.False(tool.Parameters.First(p => p.Name == "action").IsRequired);
        }

        [Fact]
        public void Execute_Pretty_FormatsJson()
        {
            var result = JsonFormatTool.Execute("{\"a\":1,\"b\":2}", "pretty");
            Assert.True(result.Success);
            Assert.Contains("\n", result.Output);
            Assert.Contains("\"a\"", result.Output);
        }

        [Fact]
        public void Execute_Minify_MinifiesJson()
        {
            var result = JsonFormatTool.Execute("{ \"a\" : 1, \"b\" : 2 }", "minify");
            Assert.True(result.Success);
            Assert.DoesNotContain(" ", result.Output);
            Assert.Equal("{\"a\":1,\"b\":2}", result.Output);
        }

        [Fact]
        public void Execute_Validate_ValidJson_ReturnsValid()
        {
            var result = JsonFormatTool.Execute("{\"key\":\"value\"}", "validate");
            Assert.True(result.Success);
            Assert.Equal("Valid JSON", result.Output);
            Assert.Equal(InferenceOutputFormats.PlainText, result.OutputFormat);
        }

        [Fact]
        public void Execute_Validate_InvalidJson_ReturnsInvalid()
        {
            var result = JsonFormatTool.Execute("not json at all", "validate");
            Assert.True(result.Success);
            Assert.StartsWith("Invalid JSON:", result.Output);
        }

        [Fact]
        public void Execute_InvalidJson_NonValidateAction_ReturnsFailure()
        {
            var result = JsonFormatTool.Execute("not json", "pretty");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ViaContext_DefaultsPretty()
        {
            var tool = new JsonFormatTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "json", Value = "{\"x\":1}", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Contains("\n", result.Output);
        }
    }
}
