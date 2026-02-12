using Daisi.Host.Core.Models;
using Daisi.Host.Core.Services;
using Daisi.Host.Core.Services.Models;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Tests.Helpers;
using System.Net;

namespace Daisi.Tools.Tests.Information
{
    /// <summary>
    /// Tests that the Gemma 3 4B model consistently selects the correct tool for each
    /// type of user query. One test per tool (11 tools total).
    /// Uses real GGUF model inference with grammar-constrained JSON output.
    /// </summary>
    [Collection("InferenceTests")]
    public class ToolInferenceTests : IDisposable
    {
        private readonly ToolInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public ToolInferenceTests(ToolInferenceFixture fixture)
        {
            _fixture = fixture;
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        #region Helper

        private static MockHttpMessageHandler CreateDummyHandler()
        {
            return new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        }

        private async Task AssertToolSelected(string prompt, string expectedToolId, params string[] requiredParamNames)
        {
            var handler = CreateDummyHandler();
            var toolSession = await _fixture.CreateToolSessionAsync(prompt, handler);

            Assert.True(toolSession.CurrentTool is not null,
                $"LLM returned null CurrentTool for prompt: '{prompt}'. Expected tool: {expectedToolId}");
            Assert.True(toolSession.CurrentTool!.Id == expectedToolId,
                $"Expected tool '{expectedToolId}' but got '{toolSession.CurrentTool.Id}' for prompt: '{prompt}'");

            // Verify all required params are present with values
            foreach (var paramName in requiredParamNames)
            {
                var param = toolSession.CurrentTool.Parameters.FirstOrDefault(p => p.Name == paramName);
                Assert.NotNull(param);
                Assert.False(string.IsNullOrWhiteSpace(param!.Value),
                    $"Required parameter '{paramName}' has no value for tool '{expectedToolId}'");
            }

            // Verify all parameter names are valid for the tool
            var toolDef = _fixture.ToolService.GetTool(expectedToolId);
            Assert.NotNull(toolDef);
            foreach (var param in toolSession.CurrentTool.Parameters)
            {
                Assert.True(
                    toolDef!.Parameters.Any(p => p.Name == param.Name),
                    $"LLM generated invalid parameter '{param.Name}' for tool '{expectedToolId}'");
            }
        }

        #endregion

        #region Tool Selection Tests — one per tool

        [Fact]
        public async Task LlmSelects_HttpGet_ForUrlFetch()
        {
            await AssertToolSelected(
                "Send an HTTP GET request to https://api.example.com/data and return the response",
                "daisi-web-clients-http-get",
                "url");
        }

        [Fact]
        public async Task LlmSelects_AnalyzeCode_ForCodeReview()
        {
            await AssertToolSelected(
                "Analyze this Python code for bugs: def divide(a, b): return a / b",
                "daisi-code-analyze",
                "code");
        }

        [Fact]
        public async Task LlmSelects_Translate_ForTranslation()
        {
            await AssertToolSelected(
                "Use the translate tool to translate the phrase 'Hello, how are you?' into Spanish language",
                "daisi-info-translate",
                "text", "target-language");
        }

        [Fact]
        public async Task LlmSelects_ListDirectory_ForDirectoryListing()
        {
            await AssertToolSelected(
                "List the files and folders in /home/user/documents",
                "daisi-files-list-directory",
                "path");
        }

        [Fact]
        public async Task LlmSelects_WriteFile_ForWritingContent()
        {
            await AssertToolSelected(
                "Save the text 'Hello World' to the file /tmp/output.txt",
                "daisi-files-write",
                "path", "content");
        }

        [Fact]
        public async Task LlmSelects_ReadFile_ForReadingFile()
        {
            await AssertToolSelected(
                "Read the file /etc/config.json and show me its contents",
                "daisi-files-read",
                "path");
        }

        [Fact]
        public async Task LlmSelects_SummarizeText_ForTextSummary()
        {
            await AssertToolSelected(
                "Summarize this text using the summarize tool: The Industrial Revolution was a period of major industrialization and innovation that took place during the late 1700s and early 1800s. It began in Great Britain and spread throughout the world.",
                "daisi-info-summarize-text",
                "text");
        }

        [Fact]
        public async Task LlmSelects_RegexMatching_ForPatternExtraction()
        {
            await AssertToolSelected(
                "Use the regex matching tool with pattern '[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}' on input: contact support@example.com or sales@company.org",
                "daisi-strings-regex-matching",
                "input", "pattern");
        }

        [Fact]
        public async Task LlmSelects_BasicMath_ForCalculation()
        {
            await AssertToolSelected(
                "Use the basic math tool to evaluate the expression: 15 * 7 + 23",
                "daisi-math-basic",
                "expression");
        }

        [Fact]
        public async Task LlmSelects_SummarizeHtml_ForHtmlSummary()
        {
            await AssertToolSelected(
                "Summarize this HTML for me: <h1>Welcome</h1><p>Our company provides software solutions for enterprise clients.</p>",
                "daisi-web-html-summarize",
                "html");
        }

        // ── New tool inference tests ──

        [Fact]
        public async Task LlmSelects_UrlEncode_ForEncodingRequest()
        {
            await AssertToolSelected(
                "Use the URL encode tool to URL-encode the string 'hello world & goodbye'",
                "daisi-strings-url-encode",
                "text");
        }

        [Fact]
        public async Task LlmSelects_JsonFormat_ForJsonFormatting()
        {
            await AssertToolSelected(
                "Pretty-print this JSON: {\"name\":\"John\",\"age\":30}",
                "daisi-strings-json-format",
                "json");
        }

        [Fact]
        public async Task LlmSelects_Base64_ForEncodingRequest()
        {
            await AssertToolSelected(
                "Base64 encode the text 'Hello World'",
                "daisi-strings-base64",
                "text");
        }

        [Fact]
        public async Task LlmSelects_DateTime_ForTimeQuery()
        {
            await AssertToolSelected(
                "Use the datetime tool to get the current date and time right now",
                "daisi-info-datetime",
                "action");
        }

        [Fact]
        public async Task LlmSelects_GenerateCode_ForCodeGeneration()
        {
            await AssertToolSelected(
                "Generate a Python function that checks if a number is prime",
                "daisi-code-generate",
                "description", "language");
        }

        [Fact]
        public async Task LlmSelects_ExplainCode_ForCodeExplanation()
        {
            await AssertToolSelected(
                "Explain this code to me: def fib(n): return n if n < 2 else fib(n-1) + fib(n-2)",
                "daisi-code-explain",
                "code");
        }

        [Fact]
        public async Task LlmSelects_UnitConvert_ForConversion()
        {
            await AssertToolSelected(
                "Use the unit convert tool to convert 100 kilometers to miles",
                "daisi-math-convert",
                "value", "from", "to");
        }

        [Fact]
        public async Task LlmSelects_HtmlToMarkdown_ForHtmlConversion()
        {
            await AssertToolSelected(
                "Convert this HTML to markdown format: <h1>Title</h1><p>Some <strong>bold</strong> text</p>",
                "daisi-web-html-to-markdown",
                "html");
        }

        [Fact]
        public async Task LlmSelects_Wikipedia_ForFactualQuery()
        {
            await AssertToolSelected(
                "Use the Wikipedia search tool to search for information about quantum computing",
                "daisi-integration-wikipedia",
                "query");
        }

        [Fact]
        public async Task LlmSelects_ImagePrompt_ForImageDescription()
        {
            await AssertToolSelected(
                "Use the image prompt tool to create an image generation prompt for a fantasy castle in the clouds",
                "daisi-media-image-prompt",
                "description");
        }

        #endregion
    }
}
