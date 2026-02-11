using Daisi.SDK.Models.Tools;
using Daisi.Tools.Strings;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Strings
{
    public class Base64ToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new Base64Tool();
            Assert.Equal("daisi-strings-base64", tool.Id);
        }

        [Fact]
        public void Parameters_TextIsRequired()
        {
            var tool = new Base64Tool();
            Assert.True(tool.Parameters.First(p => p.Name == "text").IsRequired);
        }

        [Fact]
        public void Parameters_ModeIsOptional()
        {
            var tool = new Base64Tool();
            Assert.False(tool.Parameters.First(p => p.Name == "mode").IsRequired);
        }

        [Fact]
        public void Execute_EncodeMode_EncodesCorrectly()
        {
            var result = Base64Tool.Execute("Hello, World!", "encode");
            Assert.True(result.Success);
            Assert.Equal("SGVsbG8sIFdvcmxkIQ==", result.Output);
        }

        [Fact]
        public void Execute_DecodeMode_DecodesCorrectly()
        {
            var result = Base64Tool.Execute("SGVsbG8sIFdvcmxkIQ==", "decode");
            Assert.True(result.Success);
            Assert.Equal("Hello, World!", result.Output);
        }

        [Fact]
        public void Execute_DefaultMode_Encodes()
        {
            var result = Base64Tool.Execute("test", null!);
            Assert.True(result.Success);
            Assert.Equal("dGVzdA==", result.Output);
        }

        [Fact]
        public void Execute_InvalidBase64_ReturnsFailure()
        {
            var result = Base64Tool.Execute("not-valid-base64!!!", "decode");
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void Execute_EmptyString_ReturnsEmpty()
        {
            var result = Base64Tool.Execute("", "encode");
            Assert.True(result.Success);
            Assert.Equal("", result.Output);
        }

        [Fact]
        public async Task Execute_ViaContext_Works()
        {
            var tool = new Base64Tool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "text", Value = "test", IsRequired = true },
                new() { Name = "mode", Value = "encode", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("dGVzdA==", result.Output);
        }
    }
}
