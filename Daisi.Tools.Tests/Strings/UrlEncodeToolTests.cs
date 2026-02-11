using Daisi.SDK.Models.Tools;
using Daisi.Tools.Strings;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Strings
{
    public class UrlEncodeToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new UrlEncodeTool();
            Assert.Equal("daisi-strings-url-encode", tool.Id);
        }

        [Fact]
        public void Parameters_TextIsRequired()
        {
            var tool = new UrlEncodeTool();
            Assert.True(tool.Parameters.First(p => p.Name == "text").IsRequired);
        }

        [Fact]
        public void Parameters_ModeIsOptional()
        {
            var tool = new UrlEncodeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "mode").IsRequired);
        }

        [Fact]
        public void Execute_EncodeMode_EncodesSpecialCharacters()
        {
            var result = UrlEncodeTool.Execute("hello world&foo=bar", "encode");
            Assert.True(result.Success);
            Assert.Equal("hello%20world%26foo%3Dbar", result.Output);
        }

        [Fact]
        public void Execute_DecodeMode_DecodesSpecialCharacters()
        {
            var result = UrlEncodeTool.Execute("hello%20world%26foo%3Dbar", "decode");
            Assert.True(result.Success);
            Assert.Equal("hello world&foo=bar", result.Output);
        }

        [Fact]
        public void Execute_DefaultMode_Encodes()
        {
            var result = UrlEncodeTool.Execute("test value", null!);
            Assert.True(result.Success);
            Assert.Equal("test%20value", result.Output);
        }

        [Fact]
        public void Execute_EmptyString_ReturnsEmpty()
        {
            var result = UrlEncodeTool.Execute("", "encode");
            Assert.True(result.Success);
            Assert.Equal("", result.Output);
        }

        [Fact]
        public async Task Execute_ViaContext_Works()
        {
            var tool = new UrlEncodeTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "hello world", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("hello%20world", result.Output);
        }
    }
}
