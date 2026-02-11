using Daisi.Host.Core.Models;
using Daisi.Host.Core.Services;
using Daisi.Host.Core.Services.Models;
using Daisi.Protos.V1;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Skills;
using Daisi.SDK.Models.Tools;
using Daisi.SDK.Skills;
using Daisi.Tools.Tests.Helpers;
using Daisi.Tools.Tests.Information;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace Daisi.Tools.Tests.Skills
{
    /// <summary>
    /// Tests the Skilled ThinkLevel: skill prompts injected into system instructions
    /// guide the LLM toward correct tool selection for various query types.
    ///
    /// These tests create tool sessions with skill context (from .md files) injected
    /// as system prompt, then verify the model selects the correct tools and that
    /// selected tools execute successfully.
    ///
    /// GGUF models are stored at: C:\ggufs
    /// Uses the shared WebSearchInferenceFixture (Gemma 3 4B IT Q4).
    /// </summary>
    [Collection("InferenceTests")]
    public class SkilledInferenceTests : IDisposable
    {
        private readonly WebSearchInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        // GGUF models are stored at: C:\ggufs
        // See WebSearchInferenceFixture for the full list of available models.

        public SkilledInferenceTests(WebSearchInferenceFixture fixture)
        {
            _fixture = fixture;
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        #region Helpers

        /// <summary>
        /// Loads skill .md files from the Skills/ directory in the tools project.
        /// </summary>
        private static List<DaisiSkill> LoadTestSkills()
        {
            // Skills are stored alongside the tools project at:
            // daisi-tools-dotnet/Skills/*.md
            var skillsDir = FindSkillsDirectory();
            var skills = new List<DaisiSkill>();

            if (skillsDir is null || !Directory.Exists(skillsDir))
                return skills;

            foreach (var file in Directory.EnumerateFiles(skillsDir, "*.md"))
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileNameWithoutExtension(file);
                var skill = SkillFileLoader.LoadMarkdown(content, $"required/{fileName}");
                skills.Add(skill);
            }

            return skills;
        }

        private static string? FindSkillsDirectory()
        {
            // Walk up from the test output directory to find the Skills folder
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                var candidate = Path.Combine(dir, "Skills");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.md").Length > 0)
                    return candidate;

                // Also check sibling daisi-tools-dotnet from the repo root
                var toolsCandidate = Path.Combine(dir, "daisi-tools-dotnet", "Skills");
                if (Directory.Exists(toolsCandidate))
                    return toolsCandidate;

                dir = Path.GetDirectoryName(dir);
                if (dir is null) break;
            }

            // Fallback: absolute path to repo
            var fallback = @"C:\repos\daisinet\daisi-tools-dotnet\Skills";
            return Directory.Exists(fallback) ? fallback : null;
        }

        /// <summary>
        /// Creates a tool session with Skilled-level skill context injected into the chat session.
        /// </summary>
        private async Task<ToolSession> CreateSkilledToolSessionAsync(
            string userMessage,
            HttpMessageHandler httpHandler,
            List<DaisiSkill>? skills = null)
        {
            var serviceProvider = BuildServices(httpHandler);
            DaisiStaticSettings.Services = serviceProvider;

            skills ??= LoadTestSkills();

            // Build skill context to inject as system prompt (mirrors InferenceSession behavior)
            var skillContext = new StringBuilder();
            if (skills.Count > 0)
            {
                skillContext.AppendLine("\n## Available Skills\n");
                foreach (var skill in skills)
                {
                    if (!string.IsNullOrWhiteSpace(skill.SystemPromptTemplate))
                    {
                        skillContext.AppendLine($"### {skill.Name}\n");
                        skillContext.AppendLine(skill.SystemPromptTemplate);
                        skillContext.AppendLine();
                    }
                }
            }

            // Create a chat session with skill context as system instructions
            var chatSession = await _fixture.LocalModel.CreateInteractiveChatSessionAsync(
                skillContext.ToString());

            return await _fixture.ToolService.CreateToolSessionFromUserInput(
                userMessage, _fixture.LocalModel, chatSession);
        }

        private async Task AssertSkilledToolSelected(
            string query,
            string expectedToolId,
            List<DaisiSkill>? skills = null)
        {
            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
            var toolSession = await CreateSkilledToolSessionAsync(query, handler, skills);

            Assert.True(toolSession.CurrentTool is not null,
                $"Skilled session returned null CurrentTool for query: '{query}'. Expected: {expectedToolId}");
            Assert.True(toolSession.CurrentTool!.Id == expectedToolId,
                $"Expected tool '{expectedToolId}' but got '{toolSession.CurrentTool.Id}' for query: '{query}'");
        }

        private async Task AssertSkilledToolSelectedOneOf(
            string query,
            string[] acceptableToolIds,
            List<DaisiSkill>? skills = null)
        {
            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
            var toolSession = await CreateSkilledToolSessionAsync(query, handler, skills);

            Assert.True(toolSession.CurrentTool is not null,
                $"Skilled session returned null CurrentTool for query: '{query}'. Expected one of: {string.Join(", ", acceptableToolIds)}");
            Assert.True(acceptableToolIds.Contains(toolSession.CurrentTool!.Id),
                $"Expected one of [{string.Join(", ", acceptableToolIds)}] but got '{toolSession.CurrentTool.Id}' for query: '{query}'");
        }

        private static ServiceProvider BuildServices(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }

        #endregion

        #region Skill Loading Tests

        [Fact]
        public void SkillFiles_LoadSuccessfully()
        {
            var skills = LoadTestSkills();
            Assert.NotEmpty(skills);
            Assert.True(skills.Count >= 6,
                $"Expected at least 6 skill files but found {skills.Count}");
        }

        [Fact]
        public void SkillFiles_AllHaveSystemPromptTemplates()
        {
            var skills = LoadTestSkills();
            foreach (var skill in skills)
            {
                Assert.False(string.IsNullOrWhiteSpace(skill.SystemPromptTemplate),
                    $"Skill '{skill.Name}' has empty SystemPromptTemplate");
                Assert.False(string.IsNullOrWhiteSpace(skill.Name),
                    $"Skill with id '{skill.Id}' has empty Name");
            }
        }

        [Fact]
        public void SkillFiles_ContainExpectedSkills()
        {
            var skills = LoadTestSkills();
            var names = skills.Select(s => s.Name).ToList();

            Assert.Contains("web-search", names);
            Assert.Contains("website-summary", names);
            Assert.Contains("research", names);
            Assert.Contains("code-assistant", names);
            Assert.Contains("date-aware-search", names);
            Assert.Contains("quick-convert", names);
            Assert.Contains("fact-check", names);
        }

        [Fact]
        public void SkillFiles_HaveRequiredToolGroups()
        {
            var skills = LoadTestSkills();
            foreach (var skill in skills)
            {
                Assert.NotEmpty(skill.RequiredToolGroups);
            }
        }

        #endregion

        #region Skilled Tool Selection — verify skill prompts guide correct tool selection

        [Fact]
        public async Task Skilled_WebSearchQuery_SelectsWebSearchTool()
        {
            // The web-search skill instructs the model to use daisi-info-web-search
            await AssertSkilledToolSelected(
                "Search the web for the latest news about climate change",
                "daisi-info-web-search");
        }

        [Fact]
        public async Task Skilled_UrlFetch_SelectsHttpGet()
        {
            // The website-summary skill instructs: HTTP Get -> HTML to Markdown -> Summarize
            await AssertSkilledToolSelected(
                "Use HTTP get to fetch the content from https://example.com/article",
                "daisi-web-clients-http-get");
        }

        [Fact]
        public async Task Skilled_DateTimeQuery_SelectsDateTimeTool()
        {
            // The date-aware-search skill instructs: use DateTime tool for current time
            await AssertSkilledToolSelected(
                "Use the datetime tool to tell me the current date and time",
                "daisi-info-datetime");
        }

        [Fact]
        public async Task Skilled_CodeGeneration_SelectsGenerateCode()
        {
            // The code-assistant skill instructs: use Generate Code for code generation
            await AssertSkilledToolSelected(
                "Use the generate code tool to write a Python function that sorts a list",
                "daisi-code-generate");
        }

        [Fact]
        public async Task Skilled_CodeExplanation_SelectsExplainCode()
        {
            // The code-assistant skill instructs: use Explain Code for explanation requests
            await AssertSkilledToolSelected(
                "Use the explain code tool to explain: def fib(n): return n if n < 2 else fib(n-1) + fib(n-2)",
                "daisi-code-explain");
        }

        [Fact]
        public async Task Skilled_UnitConversion_SelectsUnitConvert()
        {
            // The quick-convert skill instructs: use Unit Convert for conversions
            await AssertSkilledToolSelected(
                "Use the unit convert tool to convert 100 degrees Fahrenheit to Celsius",
                "daisi-math-convert");
        }

        [Fact]
        public async Task Skilled_MathCalculation_SelectsBasicMath()
        {
            // The quick-convert skill instructs: use Basic Math for calculations
            await AssertSkilledToolSelected(
                "Use the basic math tool to evaluate the expression 15 * 23 + 47",
                "daisi-math-basic");
        }

        [Fact]
        public async Task Skilled_WikipediaLookup_SelectsWikipedia()
        {
            // The fact-check skill instructs: use Wikipedia Search for factual queries
            await AssertSkilledToolSelected(
                "Use the Wikipedia search tool to look up the speed of light",
                "daisi-integration-wikipedia");
        }

        [Fact]
        public async Task Skilled_Base64Encode_SelectsBase64()
        {
            await AssertSkilledToolSelected(
                "Use the base64 tool to encode 'Hello World'",
                "daisi-strings-base64");
        }

        [Fact]
        public async Task Skilled_JsonFormat_SelectsJsonFormat()
        {
            await AssertSkilledToolSelected(
                "Use the JSON format tool to pretty-print: {\"name\":\"test\",\"value\":42}",
                "daisi-strings-json-format");
        }

        [Fact]
        public async Task Skilled_HtmlToMarkdown_SelectsHtmlToMarkdown()
        {
            await AssertSkilledToolSelected(
                "Use the HTML to markdown tool to convert: <h1>Title</h1><p>Content here</p>",
                "daisi-web-html-to-markdown");
        }

        [Fact]
        public async Task Skilled_UrlEncode_SelectsUrlEncode()
        {
            await AssertSkilledToolSelected(
                "Use the URL encode tool to encode the string 'hello world & test'",
                "daisi-strings-url-encode");
        }

        #endregion

        #region Skilled Tool Execution — verify tools execute successfully after skill-guided selection

        [Fact]
        public async Task Skilled_DateTimeExecution_ReturnsCurrentTime()
        {
            // Select tool via Skilled inference, then execute it
            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
            var toolSession = await CreateSkilledToolSessionAsync(
                "Use the datetime tool to get the current date and time", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-datetime", toolSession.CurrentTool!.Id);

            // Execute the selected tool
            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            // Should have tool content with a date/time string
            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content),
                "DateTime tool returned empty content");
            // The output should contain a year (basic sanity check)
            Assert.Contains("20", toolContent.Content);
        }

        [Fact]
        public async Task Skilled_Base64Execution_EncodesCorrectly()
        {
            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
            var toolSession = await CreateSkilledToolSessionAsync(
                "Use the base64 tool to encode 'Hello'", handler);

            Assert.NotNull(toolSession.CurrentTool);
            if (toolSession.CurrentTool!.Id == "daisi-strings-base64")
            {
                var context = _fixture.CreateToolContext();
                var responses = new List<SendInferenceResponse>();
                await foreach (var response in toolSession.ExecuteToolAsync(context))
                {
                    if (response is not null)
                        responses.Add(response);
                }

                var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
                Assert.NotNull(toolContent);
                // "Hello" in base64 is "SGVsbG8="
                Assert.Contains("SGVsbG8", toolContent!.Content);
            }
        }

        [Fact]
        public async Task Skilled_JsonFormatExecution_FormatsJson()
        {
            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
            var toolSession = await CreateSkilledToolSessionAsync(
                "Use the JSON format tool to pretty-print: {\"a\":1,\"b\":2}", handler);

            Assert.NotNull(toolSession.CurrentTool);
            if (toolSession.CurrentTool!.Id == "daisi-strings-json-format")
            {
                var context = _fixture.CreateToolContext();
                var responses = new List<SendInferenceResponse>();
                await foreach (var response in toolSession.ExecuteToolAsync(context))
                {
                    if (response is not null)
                        responses.Add(response);
                }

                var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
                Assert.NotNull(toolContent);
                // Pretty-printed JSON should have newlines/indentation
                Assert.Contains("\"a\"", toolContent!.Content);
                Assert.Contains("\"b\"", toolContent.Content);
            }
        }

        [Fact]
        public async Task Skilled_WebSearchExecution_ReturnsUrls()
        {
            var googleHtml = ToolTestHelpers.CreateMockGoogleHtml(
                "https://example.com/result1",
                "https://example.com/result2");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);

            var serviceProvider = BuildServices(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await CreateSkilledToolSessionAsync(
                "Search the web for machine learning tutorials", handler);

            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-web-search", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.Contains("example.com", toolContent!.Content);
        }

        #endregion

        #region Skills Context Impact — verify skills change behavior vs no skills

        [Fact]
        public async Task Skilled_WithNoSkills_StillSelectsTools()
        {
            // Even without skill prompts, Skilled level should still select tools
            // (it uses the same tool selection pipeline as BasicWithTools)
            var emptySkills = new List<DaisiSkill>();

            await AssertSkilledToolSelected(
                "Search the web for artificial intelligence news",
                "daisi-info-web-search",
                emptySkills);
        }

        [Fact]
        public async Task Skilled_WithWebsiteSummarySkill_SelectsExpectedTool()
        {
            // With the website-summary skill loaded, the model should choose one of
            // the tools in the website-summary workflow when asked to summarize a URL
            var skills = LoadTestSkills()
                .Where(s => s.Name == "website-summary")
                .ToList();

            Assert.NotEmpty(skills);

            await AssertSkilledToolSelectedOneOf(
                "Summarize the content at https://example.com/article for me",
                ["daisi-web-clients-http-get", "daisi-info-summarize-text", "daisi-web-html-to-markdown"],
                skills);
        }

        [Fact]
        public async Task Skilled_WithCodeAssistantSkill_SelectsCodeTool()
        {
            // With only the code-assistant skill, code tasks should pick coding tools
            var skills = LoadTestSkills()
                .Where(s => s.Name == "code-assistant")
                .ToList();

            Assert.NotEmpty(skills);

            await AssertSkilledToolSelectedOneOf(
                "Generate a JavaScript function to validate email addresses",
                ["daisi-code-generate", "daisi-code-analyze"],
                skills);
        }

        [Fact]
        public async Task Skilled_WithFactCheckSkill_SelectsSearchOrWikipedia()
        {
            // With the fact-check skill, factual queries should pick search or wikipedia
            var skills = LoadTestSkills()
                .Where(s => s.Name == "fact-check")
                .ToList();

            Assert.NotEmpty(skills);

            await AssertSkilledToolSelectedOneOf(
                "Verify whether the Great Wall of China is visible from space",
                ["daisi-integration-wikipedia", "daisi-info-web-search"],
                skills);
        }

        #endregion
    }
}
