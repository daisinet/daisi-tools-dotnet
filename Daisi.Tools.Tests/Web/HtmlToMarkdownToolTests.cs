using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Tests.Helpers;
using Daisi.Tools.Web.Html;

namespace Daisi.Tools.Tests.Web
{
    public class HtmlToMarkdownToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new HtmlToMarkdownTool();
            Assert.Equal("daisi-web-html-to-markdown", tool.Id);
        }

        [Fact]
        public void Parameters_HtmlIsRequired()
        {
            var tool = new HtmlToMarkdownTool();
            Assert.True(tool.Parameters.First(p => p.Name == "html").IsRequired);
        }

        [Fact]
        public void Convert_Headings()
        {
            var result = HtmlToMarkdownTool.Convert("<h1>Title</h1><h2>Subtitle</h2><h3>Section</h3>");
            Assert.Contains("# Title", result);
            Assert.Contains("## Subtitle", result);
            Assert.Contains("### Section", result);
        }

        [Fact]
        public void Convert_Links()
        {
            var result = HtmlToMarkdownTool.Convert("<a href=\"https://example.com\">Click here</a>");
            Assert.Contains("[Click here](https://example.com)", result);
        }

        [Fact]
        public void Convert_Bold()
        {
            var result = HtmlToMarkdownTool.Convert("<strong>bold text</strong>");
            Assert.Contains("**bold text**", result);
        }

        [Fact]
        public void Convert_Italic()
        {
            var result = HtmlToMarkdownTool.Convert("<em>italic text</em>");
            Assert.Contains("*italic text*", result);
        }

        [Fact]
        public void Convert_InlineCode()
        {
            var result = HtmlToMarkdownTool.Convert("<code>var x = 1;</code>");
            Assert.Contains("`var x = 1;`", result);
        }

        [Fact]
        public void Convert_ListItems()
        {
            var result = HtmlToMarkdownTool.Convert("<ul><li>Item 1</li><li>Item 2</li></ul>");
            Assert.Contains("- Item 1", result);
            Assert.Contains("- Item 2", result);
        }

        [Fact]
        public void Convert_StripScriptTags()
        {
            var result = HtmlToMarkdownTool.Convert("<p>Hello</p><script>alert('xss')</script><p>World</p>");
            Assert.DoesNotContain("alert", result);
            Assert.Contains("Hello", result);
            Assert.Contains("World", result);
        }

        [Fact]
        public void Convert_StripStyleTags()
        {
            var result = HtmlToMarkdownTool.Convert("<style>body{color:red;}</style><p>Content</p>");
            Assert.DoesNotContain("color:red", result);
            Assert.Contains("Content", result);
        }

        [Fact]
        public void Convert_HtmlEntities()
        {
            var result = HtmlToMarkdownTool.Convert("<p>A &amp; B &lt; C &gt; D</p>");
            Assert.Contains("A & B < C > D", result);
        }

        [Fact]
        public void Convert_EmptyInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, HtmlToMarkdownTool.Convert(""));
            Assert.Equal(string.Empty, HtmlToMarkdownTool.Convert("   "));
        }

        [Fact]
        public async Task Execute_ViaContext_ReturnsMarkdownFormat()
        {
            var tool = new HtmlToMarkdownTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "html", Value = "<h1>Test</h1><p>Hello world</p>", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Markdown, result.OutputFormat);
            Assert.Contains("# Test", result.Output);
        }
    }
}
