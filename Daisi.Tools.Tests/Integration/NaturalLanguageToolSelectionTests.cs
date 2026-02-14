using Daisi.SDK.Models;
using Daisi.Tools.Tests.Helpers;
using System.Net;

namespace Daisi.Tools.Tests.Integration
{
    /// <summary>
    /// Tests that the LLM picks the correct tool from natural language queries
    /// WITHOUT mentioning any tool name or tool ID. These test the UseInstructions
    /// keywords and the model's ability to map user intent to the right tool.
    ///
    /// If a tool is systematically missed, fix its UseInstructions keywords — not the prompt.
    ///
    /// GGUF models are stored at: C:\ggufs
    /// </summary>
    [Collection("InferenceTests")]
    public class NaturalLanguageToolSelectionTests : IDisposable
    {
        private readonly ToolInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public NaturalLanguageToolSelectionTests(ToolInferenceFixture fixture)
        {
            _fixture = fixture;
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        #region Helpers

        private static MockHttpMessageHandler CreateDummyHandler()
        {
            return new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        }

        private async Task AssertToolSelected(string prompt, string expectedToolId)
        {
            var handler = CreateDummyHandler();
            var toolSession = await _fixture.CreateToolSessionAsync(prompt, handler);

            Assert.True(toolSession.CurrentTool is not null,
                $"LLM returned null CurrentTool for natural prompt: '{prompt}'. Expected: {expectedToolId}");
            Assert.True(toolSession.CurrentTool!.Id == expectedToolId,
                $"Expected tool '{expectedToolId}' but got '{toolSession.CurrentTool.Id}' for natural prompt: '{prompt}'");
        }

        private async Task AssertToolSelectedOneOf(string prompt, params string[] acceptableToolIds)
        {
            var handler = CreateDummyHandler();
            var toolSession = await _fixture.CreateToolSessionAsync(prompt, handler);

            Assert.True(toolSession.CurrentTool is not null,
                $"LLM returned null CurrentTool for natural prompt: '{prompt}'. Expected one of: {string.Join(", ", acceptableToolIds)}");
            Assert.True(acceptableToolIds.Contains(toolSession.CurrentTool!.Id),
                $"Expected one of [{string.Join(", ", acceptableToolIds)}] but got '{toolSession.CurrentTool.Id}' for natural prompt: '{prompt}'");
        }

        #endregion

        #region Natural Language Selection Tests

        [Fact]
        public async Task Natural_TimeQuery_SelectsDateTime()
        {
            await AssertToolSelected(
                "What time is it right now?",
                "daisi-info-datetime");
        }

        [Fact]
        public async Task Natural_MathCalculation_SelectsBasicMath()
        {
            await AssertToolSelected(
                "What is 234 multiplied by 56?",
                "daisi-math-basic");
        }

        [Fact]
        public async Task Natural_TemperatureConversion_SelectsUnitConvert()
        {
            await AssertToolSelected(
                "How many Fahrenheit is 100 Celsius?",
                "daisi-math-convert");
        }

        [Fact]
        public async Task Natural_UrlSafe_SelectsUrlEncode()
        {
            await AssertToolSelected(
                "Make this string safe for a URL: 'hello world & test=true'",
                "daisi-strings-url-encode");
        }

        [Fact]
        public async Task Natural_Base64_SelectsBase64()
        {
            await AssertToolSelected(
                "Convert 'secret message' to base64",
                "daisi-strings-base64");
        }

        [Fact]
        public async Task Natural_JsonReadable_SelectsJsonFormat()
        {
            await AssertToolSelected(
                "Make this JSON readable: {\"users\":[{\"name\":\"Alice\"},{\"name\":\"Bob\"}]}",
                "daisi-strings-json-format");
        }

        [Fact]
        public async Task Natural_ExtractEmails_SelectsRegexMatching()
        {
            await AssertToolSelected(
                "Pull out all email addresses from this text: alice@mail.com and bob@corp.net are the contacts",
                "daisi-strings-regex-matching");
        }

        [Fact]
        public async Task Natural_FetchUrl_SelectsHttpGet()
        {
            await AssertToolSelected(
                "Fetch https://api.github.com/repos/dotnet/runtime for me",
                "daisi-web-clients-http-get");
        }

        [Fact]
        public async Task Natural_WhoIs_SelectsGrokipedia()
        {
            await AssertToolSelected(
                "Who was Albert Einstein?",
                "daisi-integration-grokipedia");
        }

        [Fact]
        public async Task Natural_SummarizePassage_SelectsSummarizeText()
        {
            await AssertToolSelected(
                "Give me a brief summary of this: The French Revolution was a period of radical political and societal change in France that began with the Estates General of 1789 and ended with the formation of the French Consulate in November 1799. Many of its ideas are considered fundamental principles of liberal democracy.",
                "daisi-info-summarize-text");
        }

        [Fact]
        public async Task Natural_Translation_SelectsTranslate()
        {
            await AssertToolSelected(
                "How do you say 'Good morning' in French?",
                "daisi-info-translate");
        }

        [Fact]
        public async Task Natural_WriteCode_SelectsGenerateCode()
        {
            await AssertToolSelected(
                "Write a JavaScript function that validates email addresses",
                "daisi-code-generate");
        }

        [Fact]
        public async Task Natural_WhatDoesCodeDo_SelectsExplainCode()
        {
            await AssertToolSelected(
                "What does this do? function f(x){return x>1?x*f(x-1):1;}",
                "daisi-code-explain");
        }

        [Fact]
        public async Task Natural_FindBugs_SelectsAnalyzeCode()
        {
            await AssertToolSelected(
                "Any bugs here? db.query('SELECT * FROM users WHERE id='+id)",
                "daisi-code-analyze");
        }

        [Fact]
        public async Task Natural_CreatePicture_SelectsImagePrompt()
        {
            await AssertToolSelected(
                "Create a picture prompt of a sunset over the ocean with dolphins jumping",
                "daisi-media-image-prompt");
        }

        [Fact]
        public async Task Natural_HtmlToText_SelectsHtmlToMarkdownOrSummarize()
        {
            await AssertToolSelectedOneOf(
                "Turn this into plain text: <h2>Chapter 1</h2><p>It was a dark and stormy night.</p>",
                "daisi-web-html-to-markdown", "daisi-web-html-summarize");
        }

        [Fact]
        public async Task Natural_WebpageAbout_SelectsSummarizeHtml()
        {
            await AssertToolSelectedOneOf(
                "What is this webpage about? <html><head><title>Tech Blog</title></head><body><h1>AI Revolution</h1><p>Artificial intelligence is transforming every industry from healthcare to finance.</p></body></html>",
                "daisi-web-html-summarize", "daisi-web-html-to-markdown");
        }

        [Fact]
        public async Task Natural_ListFiles_SelectsListDirectory()
        {
            await AssertToolSelected(
                "What files are in C:\\Users\\test\\Downloads?",
                "daisi-files-list-directory");
        }

        [Fact]
        public async Task Natural_CreateFile_SelectsWriteFile()
        {
            await AssertToolSelected(
                "Create a file at C:\\temp\\notes.txt with the content 'Remember to buy groceries'",
                "daisi-files-write");
        }

        [Fact]
        public async Task Natural_ShowFileContents_SelectsReadFile()
        {
            await AssertToolSelected(
                "Show me what's inside C:\\config\\settings.json",
                "daisi-files-read");
        }

        #endregion
    }
}
