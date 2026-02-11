using Daisi.Host.Core.Models;
using Daisi.Host.Core.Services;
using Daisi.Host.Core.Services.Models;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Tools;
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
        private readonly WebSearchInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public ToolInferenceTests(WebSearchInferenceFixture fixture)
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
        public async Task LlmSelects_WebSearch_ForSearchQuery()
        {
            await AssertToolSelected(
                "Search the web for the latest news about artificial intelligence",
                "daisi-info-web-search",
                "query");
        }

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
                "Translate 'Hello, how are you?' into Spanish",
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
                "Summarize the following text: The Industrial Revolution was a period of major industrialization and innovation that took place during the late 1700s and early 1800s. It began in Great Britain and spread throughout the world.",
                "daisi-info-summarize-text",
                "text");
        }

        [Fact]
        public async Task LlmSelects_RegexMatching_ForPatternExtraction()
        {
            await AssertToolSelected(
                "Use regex to extract all email addresses from: contact support@example.com or sales@company.org",
                "daisi-strings-regex-matching",
                "input", "pattern");
        }

        [Fact]
        public async Task LlmSelects_BasicMath_ForCalculation()
        {
            await AssertToolSelected(
                "Calculate the result of 15 * 7 + 23",
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

        #endregion
    }
}
