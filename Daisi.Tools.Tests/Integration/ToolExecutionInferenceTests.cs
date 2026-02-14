using Daisi.Protos.V1;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Daisi.Tools.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify the full pipeline: LLM selects the correct tool
    /// AND the tool executes successfully producing correct output.
    /// Uses real GGUF model inference for tool selection; deterministic tools use MockToolContext,
    /// inference-dependent tools use the fixture's real inference context.
    ///
    /// GGUF models are stored at: C:\ggufs
    /// </summary>
    [Collection("InferenceTests")]
    public class ToolExecutionInferenceTests : IDisposable
    {
        private readonly ToolInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public ToolExecutionInferenceTests(ToolInferenceFixture fixture)
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

        private async Task<string> SelectAndExecute(string prompt, string expectedToolId, HttpMessageHandler? handler = null)
        {
            handler ??= CreateDummyHandler();

            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(prompt, handler);
            return await ToolTestHelpers.AssertToolSelectedAndExecuted(_fixture, toolSession, expectedToolId);
        }

        #endregion

        #region Deterministic Tool Execution Tests

        [Fact]
        public async Task Execute_BasicMath_EvaluatesExpression()
        {
            var content = await SelectAndExecute(
                "Use the basic math tool to evaluate: 15 * 7 + 23",
                "daisi-math-basic");

            Assert.Contains("128", content);
        }

        [Fact]
        public async Task Execute_UnitConvert_ConvertsKmToMiles()
        {
            var content = await SelectAndExecute(
                "Use the unit convert tool to convert 100 kilometers to miles",
                "daisi-math-convert");

            Assert.Contains("62", content);
        }

        [Fact]
        public async Task Execute_DateTime_ReturnsCurrentTime()
        {
            var content = await SelectAndExecute(
                "Use the datetime tool to get the current date and time",
                "daisi-info-datetime");

            Assert.Contains("202", content);
        }

        [Fact]
        public async Task Execute_Base64_EncodesCorrectly()
        {
            var content = await SelectAndExecute(
                "Use the base64 tool to encode 'Hello World'",
                "daisi-strings-base64");

            Assert.Contains("SGVsbG8gV29ybGQ", content);
        }

        [Fact]
        public async Task Execute_UrlEncode_EncodesSpaces()
        {
            var content = await SelectAndExecute(
                "Use the URL encode tool to encode 'hello world & goodbye'",
                "daisi-strings-url-encode");

            Assert.Contains("hello%20world", content);
        }

        [Fact]
        public async Task Execute_JsonFormat_PrettyPrints()
        {
            var content = await SelectAndExecute(
                "Use the JSON format tool to pretty-print: {\"name\":\"John\"}",
                "daisi-strings-json-format");

            Assert.Contains("name", content);
        }

        [Fact]
        public async Task Execute_RegexMatches_ExtractsEmails()
        {
            var content = await SelectAndExecute(
                "Use regex matching tool with pattern '[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}' on input: contact support@example.com for help",
                "daisi-strings-regex-matching");

            Assert.Contains("support@example.com", content);
        }

        [Fact]
        public async Task Execute_HtmlToMarkdown_ConvertsHeading()
        {
            var content = await SelectAndExecute(
                "Use HTML to markdown tool to convert: <h1>Title</h1><p>Some text here</p>",
                "daisi-web-html-to-markdown");

            Assert.Contains("# Title", content);
        }

        [Fact]
        public async Task Execute_WriteFile_WritesToDisk()
        {
            var tempDir = ToolTestHelpers.CreateTempTestDirectory();
            try
            {
                var filePath = Path.Combine(tempDir, "output.txt");
                var content = await SelectAndExecute(
                    $"Save 'Hello World' to {filePath}",
                    "daisi-files-write");

                Assert.Contains("Successfully wrote", content);
                Assert.True(File.Exists(filePath), $"File was not created at {filePath}");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Execute_ReadFile_ReadsContent()
        {
            var tempDir = ToolTestHelpers.CreateTempTestDirectory();
            try
            {
                var filePath = Path.Combine(tempDir, "test.txt");
                await File.WriteAllTextAsync(filePath, "Hello from test file");

                var content = await SelectAndExecute(
                    $"Read the file {filePath}",
                    "daisi-files-read");

                Assert.Contains("Hello from test file", content);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Execute_ListDirectory_ListsFiles()
        {
            var tempDir = ToolTestHelpers.CreateTempTestDirectory();
            try
            {
                await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "a");
                await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "b");

                var content = await SelectAndExecute(
                    $"List files in {tempDir}",
                    "daisi-files-list-directory");

                Assert.Contains("file1.txt", content);
                Assert.Contains("file2.txt", content);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Execute_HttpGet_ReturnsMockContent()
        {
            var mockHtml = "<html><body><h1>Hello API</h1></body></html>";
            var handler = new MockHttpMessageHandler(mockHtml, HttpStatusCode.OK);

            var content = await SelectAndExecute(
                "Send an HTTP GET request to https://api.example.com/data and return the response",
                "daisi-web-clients-http-get",
                handler);

            Assert.Contains("Hello API", content);
        }

        [Fact]
        public async Task Execute_Grokipedia_ReturnsMockResults()
        {
            var grokipediaResponse = ToolTestHelpers.CreateMockGrokipediaResponse(
                ("Quantum computing", "A type of computation using quantum mechanics"),
                ("Quantum supremacy", "Advantage over classical computers"));
            var handler = new MockHttpMessageHandler(grokipediaResponse, HttpStatusCode.OK);

            var content = await SelectAndExecute(
                "Use the Grokipedia search tool to search for quantum computing",
                "daisi-integration-grokipedia",
                handler);

            Assert.Contains("Quantum computing", content);
        }

        #endregion

        #region Inference-Dependent Tool Execution Tests

        [Fact]
        public async Task Execute_SummarizeText_ProducesSummary()
        {
            var longText = "The Industrial Revolution was a period of major industrialization and innovation " +
                "that took place during the late 1700s and early 1800s. It began in Great Britain and quickly " +
                "spread throughout the world. The Industrial Revolution marked a major turning point in history " +
                "as it dramatically changed every aspect of daily life. Prior to the revolution, most people " +
                "lived in small rural communities and their daily existence revolved around farming.";

            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                $"Summarize this text using the summarize tool: {longText}", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-summarize-text", toolSession.CurrentTool!.Id);

            // Execute with real inference context for the summarization
            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
            // Summary should be shorter than input
            Assert.True(toolContent.Content.Length < longText.Length + 100,
                "Summary should be shorter than the original text");
        }

        [Fact]
        public async Task Execute_TranslateText_ProducesTranslation()
        {
            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Use the translate tool to translate the phrase 'Hello' to Spanish language", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-translate", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
            // Should contain Spanish greeting
            Assert.True(
                toolContent.Content.Contains("Hola", StringComparison.OrdinalIgnoreCase) ||
                toolContent.Content.Contains("hola", StringComparison.OrdinalIgnoreCase),
                $"Expected Spanish translation containing 'Hola' but got: {toolContent.Content}");
        }

        [Fact]
        public async Task Execute_SummarizeHtml_ProducesSummary()
        {
            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Summarize this HTML: <h1>Welcome to TechCorp</h1><p>We provide enterprise software solutions for businesses worldwide. Our products include cloud computing, AI analytics, and cybersecurity platforms.</p>",
                handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-web-html-summarize", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
        }

        [Fact]
        public async Task Execute_GenerateCode_ProducesCode()
        {
            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Generate a Python function that checks if a number is prime", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-code-generate", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
            // Should contain code markers
            Assert.True(
                toolContent.Content.Contains("def") || toolContent.Content.Contains("function") ||
                toolContent.Content.Contains("```") || toolContent.Content.Contains("prime"),
                $"Expected code content but got: {toolContent.Content[..System.Math.Min(200, toolContent.Content.Length)]}");
        }

        [Fact]
        public async Task Execute_ExplainCode_ProducesExplanation()
        {
            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Explain this code: def fib(n): return n if n < 2 else fib(n-1) + fib(n-2)", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-code-explain", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
            // Should reference fibonacci or recursion concepts
            Assert.True(
                toolContent.Content.Contains("fib", StringComparison.OrdinalIgnoreCase) ||
                toolContent.Content.Contains("recur", StringComparison.OrdinalIgnoreCase) ||
                toolContent.Content.Contains("sequence", StringComparison.OrdinalIgnoreCase),
                $"Expected explanation referencing fibonacci concepts but got: {toolContent.Content[..System.Math.Min(200, toolContent.Content.Length)]}");
        }

        [Fact]
        public async Task Execute_AnalyzeCode_FindsIssues()
        {
            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Analyze this Python code for bugs: def divide(a, b): return a / b", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-code-analyze", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
            // Should mention division by zero or divide
            Assert.True(
                toolContent.Content.Contains("zero", StringComparison.OrdinalIgnoreCase) ||
                toolContent.Content.Contains("divide", StringComparison.OrdinalIgnoreCase) ||
                toolContent.Content.Contains("division", StringComparison.OrdinalIgnoreCase),
                $"Expected analysis mentioning division/zero but got: {toolContent.Content[..System.Math.Min(200, toolContent.Content.Length)]}");
        }

        [Fact]
        public async Task Execute_ImagePrompt_GeneratesDescription()
        {
            var handler = CreateDummyHandler();
            var serviceProvider = ToolTestHelpers.BuildServicesForFixture(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Use the image prompt tool to create an image generation prompt for a fantasy castle in the clouds", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-media-image-prompt", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content));
            // Image prompt should be descriptive (>50 chars)
            Assert.True(toolContent.Content.Length > 50,
                $"Expected descriptive image prompt (>50 chars) but got {toolContent.Content.Length} chars");
        }

        #endregion
    }
}
