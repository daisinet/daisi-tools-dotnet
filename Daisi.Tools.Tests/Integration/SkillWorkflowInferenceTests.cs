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

namespace Daisi.Tools.Tests.Integration
{
    /// <summary>
    /// Tests multi-tool chaining via skill workflows. Each test loads a specific skill,
    /// verifies the first tool is selected correctly, executes it, adds the result to
    /// history, then verifies the second tool is selected via GetNextToolAsync().
    ///
    /// GGUF models are stored at: C:\ggufs
    /// </summary>
    [Collection("InferenceTests")]
    public class SkillWorkflowInferenceTests : IDisposable
    {
        private readonly WebSearchInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public SkillWorkflowInferenceTests(WebSearchInferenceFixture fixture)
        {
            _fixture = fixture;
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        #region Helpers

        private static List<DaisiSkill> LoadTestSkills()
        {
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
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                var candidate = Path.Combine(dir, "Skills");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.md").Length > 0)
                    return candidate;

                var toolsCandidate = Path.Combine(dir, "daisi-tools-dotnet", "Skills");
                if (Directory.Exists(toolsCandidate))
                    return toolsCandidate;

                dir = Path.GetDirectoryName(dir);
                if (dir is null) break;
            }

            var fallback = @"C:\repos\daisinet\daisi-tools-dotnet\Skills";
            return Directory.Exists(fallback) ? fallback : null;
        }

        private async Task<ToolSession> CreateSkilledToolSessionAsync(
            string userMessage,
            HttpMessageHandler httpHandler,
            List<DaisiSkill>? skills = null)
        {
            var serviceProvider = BuildServices(httpHandler);
            DaisiStaticSettings.Services = serviceProvider;

            skills ??= LoadTestSkills();

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

            var chatSession = await _fixture.LocalModel.CreateInteractiveChatSessionAsync(
                skillContext.ToString());

            return await _fixture.ToolService.CreateToolSessionFromUserInput(
                userMessage, _fixture.LocalModel, chatSession);
        }

        private List<DaisiSkill> LoadSkillsByName(params string[] skillNames)
        {
            return LoadTestSkills()
                .Where(s => skillNames.Contains(s.Name))
                .ToList();
        }

        private static ServiceProvider BuildServices(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Executes the current tool and returns the ToolContent output string.
        /// </summary>
        private async Task<string> ExecuteCurrentTool(ToolSession toolSession)
        {
            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            return toolContent?.Content ?? string.Empty;
        }

        #endregion

        #region Skill Workflow Tests

        [Fact]
        public async Task Workflow_WebsiteSummary_HttpGetThenConvertOrSummarize()
        {
            // website-summary: HttpGet → HtmlToMarkdown or SummarizeText
            var skills = LoadSkillsByName("website-summary");
            Assert.NotEmpty(skills);

            var mockHtml = "<html><body><h1>Article Title</h1><p>This is the article content about technology trends.</p></body></html>";
            var handler = new MockHttpMessageHandler(mockHtml, HttpStatusCode.OK);

            var toolSession = await CreateSkilledToolSessionAsync(
                "Summarize the content at https://example.com/article",
                handler, skills);

            // First tool should be HttpGet (to fetch the URL)
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-web-clients-http-get", toolSession.CurrentTool!.Id);

            // Execute first tool and add result to history
            var firstOutput = await ExecuteCurrentTool(toolSession);
            Assert.False(string.IsNullOrWhiteSpace(firstOutput));
            toolSession.AddToolResultToHistory(firstOutput);

            // Get next tool — should be HtmlToMarkdown or SummarizeText or SummarizeHtml
            await toolSession.GetNextToolAsync();
            Assert.NotNull(toolSession.CurrentTool);
            Assert.True(
                toolSession.CurrentTool!.Id == "daisi-web-html-to-markdown" ||
                toolSession.CurrentTool.Id == "daisi-info-summarize-text" ||
                toolSession.CurrentTool.Id == "daisi-web-html-summarize",
                $"Expected html-to-markdown, summarize-text, or html-summarize but got '{toolSession.CurrentTool.Id}'");
        }

        [Fact]
        public async Task Workflow_DateAwareSearch_DateTimeThenWebSearch()
        {
            // date-aware-search: DateTime → WebSearch
            var skills = LoadSkillsByName("date-aware-search");
            Assert.NotEmpty(skills);

            var googleHtml = ToolTestHelpers.CreateMockGoogleHtml(
                "https://example.com/ai-regulation-2026");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);

            var toolSession = await CreateSkilledToolSessionAsync(
                "What are the latest AI regulation news?",
                handler, skills);

            // First tool should be DateTime (to get current date)
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-datetime", toolSession.CurrentTool!.Id);

            // Execute DateTime and add result to history
            var dateOutput = await ExecuteCurrentTool(toolSession);
            Assert.False(string.IsNullOrWhiteSpace(dateOutput));
            toolSession.AddToolResultToHistory(dateOutput);

            // Next tool should be WebSearch
            await toolSession.GetNextToolAsync();
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-web-search", toolSession.CurrentTool!.Id);
        }

        [Fact]
        public async Task Workflow_Research_WebSearchThenHttpGet()
        {
            // research: WebSearch → HttpGet
            var skills = LoadSkillsByName("research");
            Assert.NotEmpty(skills);

            var googleHtml = ToolTestHelpers.CreateMockGoogleHtml(
                "https://example.com/quantum-research");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);

            var toolSession = await CreateSkilledToolSessionAsync(
                "Research quantum computing advances",
                handler, skills);

            // First tool should be WebSearch
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-web-search", toolSession.CurrentTool!.Id);

            // Execute WebSearch and add result to history
            var searchOutput = await ExecuteCurrentTool(toolSession);
            Assert.False(string.IsNullOrWhiteSpace(searchOutput));
            toolSession.AddToolResultToHistory(searchOutput);

            // Next tool should be HttpGet (to fetch one of the URLs)
            await toolSession.GetNextToolAsync();
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-web-clients-http-get", toolSession.CurrentTool!.Id);
        }

        [Fact]
        public async Task Workflow_FactCheck_WikipediaOrWebSearchThenTheOther()
        {
            // fact-check: Wikipedia or WebSearch → the other
            var skills = LoadSkillsByName("fact-check");
            Assert.NotEmpty(skills);

            var wikiResponse = ToolTestHelpers.CreateMockWikipediaResponse(
                ("Great Wall of China", "Ancient fortification in northern China"));
            var handler = new MockHttpMessageHandler(wikiResponse, HttpStatusCode.OK);

            var toolSession = await CreateSkilledToolSessionAsync(
                "Is the Great Wall of China visible from space?",
                handler, skills);

            // First tool should be Wikipedia or WebSearch
            Assert.NotNull(toolSession.CurrentTool);
            var firstToolId = toolSession.CurrentTool!.Id;
            Assert.True(
                firstToolId == "daisi-integration-wikipedia" || firstToolId == "daisi-info-web-search",
                $"Expected wikipedia or web-search as first tool but got '{firstToolId}'");

            // Execute first tool and add result to history
            var firstOutput = await ExecuteCurrentTool(toolSession);
            Assert.False(string.IsNullOrWhiteSpace(firstOutput));
            toolSession.AddToolResultToHistory(firstOutput);

            // Next tool should be the other one (or another valid research tool)
            await toolSession.GetNextToolAsync();
            if (toolSession.CurrentTool is not null)
            {
                Assert.True(
                    toolSession.CurrentTool.Id == "daisi-integration-wikipedia" ||
                    toolSession.CurrentTool.Id == "daisi-info-web-search" ||
                    toolSession.CurrentTool.Id == "daisi-web-clients-http-get",
                    $"Expected wikipedia, web-search, or http-get as second tool but got '{toolSession.CurrentTool.Id}'");
            }
            // It's acceptable if the model decides no more tools are needed after the first result
        }

        [Fact]
        public async Task Workflow_CodeAssistant_GenerateCodeThenAnalyze()
        {
            // code-assistant: GenerateCode → AnalyzeCode
            var skills = LoadSkillsByName("code-assistant");
            Assert.NotEmpty(skills);

            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);

            var toolSession = await CreateSkilledToolSessionAsync(
                "Write a Python sort function and then review it for bugs",
                handler, skills);

            // First tool should be GenerateCode
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-code-generate", toolSession.CurrentTool!.Id);

            // Execute GenerateCode with real inference and add result to history
            var codeOutput = await ExecuteCurrentTool(toolSession);
            Assert.False(string.IsNullOrWhiteSpace(codeOutput));
            toolSession.AddToolResultToHistory(codeOutput);

            // Next tool should be AnalyzeCode
            await toolSession.GetNextToolAsync();
            Assert.NotNull(toolSession.CurrentTool);
            Assert.True(
                toolSession.CurrentTool!.Id == "daisi-code-analyze" ||
                toolSession.CurrentTool.Id == "daisi-code-explain",
                $"Expected code-analyze or code-explain as second tool but got '{toolSession.CurrentTool.Id}'");
        }

        [Fact]
        public async Task Workflow_QuickConvert_BasicMathThenUnitConvert()
        {
            // quick-convert: BasicMath → UnitConvert
            var skills = LoadSkillsByName("quick-convert");
            Assert.NotEmpty(skills);

            var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);

            var toolSession = await CreateSkilledToolSessionAsync(
                "How many miles is 5km + 10km?",
                handler, skills);

            // First tool should be BasicMath (to add 5+10)
            Assert.NotNull(toolSession.CurrentTool);
            Assert.True(
                toolSession.CurrentTool!.Id == "daisi-math-basic" ||
                toolSession.CurrentTool.Id == "daisi-math-convert",
                $"Expected basic-math or unit-convert as first tool but got '{toolSession.CurrentTool.Id}'");

            if (toolSession.CurrentTool.Id == "daisi-math-basic")
            {
                // Execute BasicMath and add result to history
                var mathOutput = await ExecuteCurrentTool(toolSession);
                Assert.False(string.IsNullOrWhiteSpace(mathOutput));
                toolSession.AddToolResultToHistory(mathOutput);

                // Next tool should be UnitConvert
                await toolSession.GetNextToolAsync();
                Assert.NotNull(toolSession.CurrentTool);
                Assert.Equal("daisi-math-convert", toolSession.CurrentTool!.Id);
            }
            // If it picked UnitConvert first, that's also an acceptable path
        }

        #endregion
    }
}
