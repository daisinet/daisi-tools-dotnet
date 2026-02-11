using Daisi.Host.Core.Models;
using Daisi.Host.Core.Services;
using Daisi.Host.Core.Services.Interfaces;
using Daisi.Host.Core.Services.Models;
using Daisi.Protos.V1;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Google.Protobuf;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LLama.Common.ChatHistory;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Daisi.Tools.Tests.Information
{
    /// <summary>
    /// Collection definition that ensures all inference tests share a single model fixture
    /// and run sequentially (they share DaisiStaticSettings.Services global state).
    /// </summary>
    [CollectionDefinition("InferenceTests")]
    public class InferenceTestCollection : ICollectionFixture<WebSearchInferenceFixture> { }

    /// <summary>
    /// Real inference tests that load the Gemma 3 4B GGUF model and run actual LLM inference
    /// through the full tool pipeline. These tests verify:
    /// 1. The model loads and runs inference successfully
    /// 2. The LLM can select tools when prompted
    /// 3. Selected tools execute and produce correct results
    /// 4. The WebSearchTool works through the real host pipeline
    ///
    /// Mock HTTP is used so tests don't hit real Google, but everything else is real:
    /// real GGUF model, real LLamaSharp inference, real tool pipeline.
    /// </summary>
    [Collection("InferenceTests")]
    public class WebSearchInferenceTests : IDisposable
    {
        private readonly WebSearchInferenceFixture _fixture;
        private readonly IServiceProvider? _originalServices;

        public WebSearchInferenceTests(WebSearchInferenceFixture fixture)
        {
            _fixture = fixture;
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        #region Model & ToolService Tests

        [Fact]
        public void ModelLoaded_Successfully()
        {
            // The GGUF model loaded without errors during fixture initialization
            Assert.NotNull(_fixture.LocalModel);
            Assert.NotNull(_fixture.LocalModel.Weights);
            Assert.True(_fixture.LocalModel.FileExists);
        }

        [Fact]
        public void ToolService_DiscoversWebSearchTool()
        {
            // ToolService.LoadTools() found the WebSearchTool via reflection
            var tool = _fixture.ToolService.GetTool("daisi-info-web-search");
            Assert.NotNull(tool);
            Assert.Equal("Daisi Web Search", tool!.Name);
        }

        [Fact]
        public void ToolService_CachedToolsJsonIncludesWebSearch()
        {
            // The cached tools JSON (sent to LLM for tool selection) includes web search
            Assert.NotNull(_fixture.ToolService.CachedToolsJson);
            Assert.Contains("daisi-info-web-search", _fixture.ToolService.CachedToolsJson!);
            Assert.Contains("query", _fixture.ToolService.CachedToolsJson);
        }

        #endregion

        #region LLM Tool Selection via Real Inference

        [Fact]
        public async Task RealInference_LlmSelectsWebSearchTool()
        {
            // The LLM MUST select the web search tool for a search query.
            var handler = CreateMockGoogleHandler("https://example.com/result");

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Search the web for the latest news about artificial intelligence", handler);

            // Tool MUST be selected
            Assert.NotNull(toolSession.CurrentTool);

            // Must be the web search tool specifically
            Assert.Equal("daisi-info-web-search", toolSession.CurrentTool!.Id);

            // Must have valid parameters
            Assert.NotNull(toolSession.CurrentTool.Parameters);
            Assert.NotEmpty(toolSession.CurrentTool.Parameters);

            // Must include the required "query" parameter with a value
            var queryParam = toolSession.CurrentTool.Parameters.FirstOrDefault(p => p.Name == "query");
            Assert.NotNull(queryParam);
            Assert.False(string.IsNullOrWhiteSpace(queryParam!.Value));
        }

        [Fact]
        public async Task RealInference_SelectedToolParametersAreValid()
        {
            // The LLM MUST select web search with valid parameters for an info query.
            var handler = CreateMockGoogleHandler("https://example.com/result");

            var toolSession = await _fixture.CreateToolSessionAsync(
                "Find information about quantum computing breakthroughs", handler);

            // Tool MUST be selected
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-web-search", toolSession.CurrentTool!.Id);

            var selectedTool = _fixture.ToolService.GetTool(toolSession.CurrentTool.Id);
            Assert.NotNull(selectedTool);

            // All parameter names must be valid
            foreach (var param in toolSession.CurrentTool.Parameters)
            {
                var validParam = selectedTool!.Parameters.Any(p => p.Name == param.Name);
                Assert.True(validParam,
                    $"LLM generated invalid parameter '{param.Name}' for tool '{toolSession.CurrentTool.Id}'");
            }

            // All required parameters must have values
            foreach (var requiredParam in selectedTool!.Parameters.Where(p => p.IsRequired))
            {
                var provided = toolSession.CurrentTool.Parameters
                    .FirstOrDefault(p => p.Name == requiredParam.Name);
                Assert.NotNull(provided);
                Assert.False(string.IsNullOrWhiteSpace(provided!.Value),
                    $"Required parameter '{requiredParam.Name}' has no value");
            }
        }

        [Fact]
        public async Task RealInference_ExecuteSelectedTool_ProducesResults()
        {
            // Full pipeline: LLM selects tool -> execute it -> verify URL results
            var handler = CreateMockGoogleHandler(
                "https://en.wikipedia.org/wiki/Paris",
                "https://www.britannica.com/place/Paris");

            var serviceProvider = BuildHostServicesWithMockHttp(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var toolSession = await _fixture.CreateToolSessionAsync(
                "What are the main tourist attractions in Paris?", handler);

            // Tool MUST be selected
            Assert.NotNull(toolSession.CurrentTool);
            Assert.Equal("daisi-info-web-search", toolSession.CurrentTool!.Id);

            var context = _fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                responses.Add(response);
            }

            // Must have Tooling responses (tool execution messages)
            Assert.Contains(responses, r => r.Type == InferenceResponseTypes.Tooling);

            // Must have ToolContent with the search result URLs
            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.Contains("wikipedia.org", toolContent!.Content);
            Assert.Contains("britannica.com", toolContent.Content);

            // Mock HTTP was called, proving tool executed network request
            Assert.NotNull(handler.LastRequest);
        }

        #endregion

        #region Direct WebSearchTool Execution (deterministic, no LLM stochasticity)

        [Fact]
        public async Task DirectExecution_WebSearchTool_ReturnsUrls()
        {
            // Execute WebSearchTool directly through the host pipeline
            // (DI, DefaultToolContext, DaisiStaticSettings) — no LLM involvement
            var expectedUrls = new[] {
                "https://www.python.org/downloads/",
                "https://en.wikipedia.org/wiki/Python_(programming_language)",
                "https://realpython.com/python-news/"
            };

            var googleHtml = CreateRealisticGoogleHtml("Python latest version",
                ("https://www.python.org/downloads/", "Download Python"),
                ("https://en.wikipedia.org/wiki/Python_(programming_language)", "Python - Wikipedia"),
                ("https://realpython.com/python-news/", "What's New"));

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServicesWithMockHttp(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var tool = _fixture.ToolService.GetTool("daisi-info-web-search")!;
            var context = _fixture.CreateToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "Python latest version", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(urls);
            Assert.Equal(3, urls!.Length);
            foreach (var expectedUrl in expectedUrls)
            {
                Assert.Contains(expectedUrl, urls);
            }
        }

        [Fact]
        public async Task DirectExecution_WebSearchTool_FiltersGoogleAndYouTube()
        {
            // Verify URL filtering works through the full host pipeline
            var googleHtml = @"
<!DOCTYPE html>
<html><body>
<div id=""search"">
  <a href=""/url?q=https://www.google.com/settings&amp;sa=U"">Settings</a>
  <a href=""/url?q=https://youtube.com/watch?v=abc&amp;sa=U"">Video</a>
  <a href=""/url?q=https://example.com/real-result&amp;sa=U"">Real</a>
</div>
</body></html>";

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServicesWithMockHttp(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var tool = _fixture.ToolService.GetTool("daisi-info-web-search")!;
            var context = _fixture.CreateToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Single(urls!);
            Assert.Equal("https://example.com/real-result", urls![0]);
        }

        [Fact]
        public async Task DirectExecution_WebSearchTool_SetsUserAgent()
        {
            var googleHtml = CreateRealisticGoogleHtml("test",
                ("https://example.com/r", "Result"));

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServicesWithMockHttp(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var tool = _fixture.ToolService.GetTool("daisi-info-web-search")!;
            var context = _fixture.CreateToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.NotNull(handler.LastRequest);
            var ua = handler.LastRequest!.Headers.UserAgent.ToString();
            Assert.Contains("Mozilla/5.0", ua);
            Assert.Contains("AppleWebKit", ua);
        }

        [Fact]
        public async Task DirectExecution_WebSearchTool_TargetsGoogleSearch()
        {
            var googleHtml = CreateRealisticGoogleHtml("test",
                ("https://example.com/r", "Result"));

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServicesWithMockHttp(handler);
            DaisiStaticSettings.Services = serviceProvider;

            var tool = _fixture.ToolService.GetTool("daisi-info-web-search")!;
            var context = _fixture.CreateToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "artificial intelligence", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            await execContext.ExecutionTask;

            Assert.NotNull(handler.LastRequest);
            var uri = handler.LastRequest!.RequestUri!;
            Assert.Equal("www.google.com", uri.Host);
            Assert.Equal("/search", uri.AbsolutePath);
            Assert.Contains("q=", uri.Query);
        }

        #endregion

        #region Helpers

        private static MockHttpMessageHandler CreateMockGoogleHandler(params string[] urls)
        {
            var links = string.Join("\n", urls.Select(u =>
                $@"<a href=""/url?q={u}&amp;sa=U&amp;ved=2ahUKE"">Result</a>"));
            var html = $@"<!DOCTYPE html><html><body><div id=""search"">{links}</div></body></html>";
            return new MockHttpMessageHandler(html, HttpStatusCode.OK);
        }

        private static string CreateRealisticGoogleHtml(string query, params (string url, string title)[] results)
        {
            var links = string.Join("\n", results.Select(r =>
                $@"<div class=""g"">
                    <a href=""/url?q={r.url}&amp;sa=U&amp;ved=2ahUKE"">
                        <h3>{r.title}</h3>
                    </a>
                </div>"));
            return $@"
<!DOCTYPE html>
<html><head><title>{query} - Google Search</title></head>
<body>
<div id=""search"">
{links}
</div>
</body></html>";
        }

        private static ServiceProvider BuildHostServicesWithMockHttp(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }

        #endregion
    }

    /// <summary>
    /// Shared test fixture that loads the Gemma 3 4B GGUF model once and reuses it
    /// across all inference tests.
    /// </summary>
    public class WebSearchInferenceFixture : IAsyncLifetime
    {
        private const string ModelFolder = @"C:\GGUFs";
        private const string ModelFileName = "gemma-3-4b-it-UD-Q4_K_XL.gguf";

        public LocalModel LocalModel { get; private set; } = null!;
        public ToolService ToolService { get; private set; } = null!;

        private ServiceProvider? _serviceProvider;
        private IServiceProvider? _originalStaticServices;

        public async Task InitializeAsync()
        {
            _originalStaticServices = DaisiStaticSettings.Services;

            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

            var settings = new Settings
            {
                Model = new ModelSettings
                {
                    ModelFolderPath = ModelFolder,
                    LLama = new LLamaSettings
                    {
                        Runtime = LLamaRuntimes.Auto,
                        ContextSize = 4096,
                        GpuLayerCount = -1,
                        BatchSize = 128
                    }
                }
            };

            var settingsService = new TestSettingsService(settings, ModelFolder);

            NativeLibraryConfig.All.WithAutoFallback(true);

            var aiModel = new AIModel
            {
                Name = "Gemma 3 4B IT Q4",
                FileName = ModelFileName
            };

            var modelLogger = loggerFactory.CreateLogger<LocalModel>();
            LocalModel = new LocalModel(aiModel, modelLogger, settingsService);
            LocalModel.Load();

            var toolServiceLogger = loggerFactory.CreateLogger<ToolService>();
            ToolService = new ToolService(settingsService, toolServiceLogger);
            ToolService.LoadTools();

            if (ToolService.GetTool("daisi-info-web-search") is null)
                throw new InvalidOperationException("WebSearchTool was not discovered by ToolService.LoadTools()");

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (LocalModel is not null)
                await LocalModel.DisposeAsync();

            DaisiStaticSettings.Services = _originalStaticServices!;
            _serviceProvider?.Dispose();
        }

        public async Task<ToolSession> CreateToolSessionAsync(string userMessage, HttpMessageHandler httpHandler)
        {
            var serviceProvider = BuildServices(httpHandler);
            _serviceProvider = serviceProvider;
            DaisiStaticSettings.Services = serviceProvider;

            var userSession = await LocalModel.CreateInteractiveChatSessionAsync();
            return await ToolService.CreateToolSessionFromUserInput(userMessage, LocalModel, userSession);
        }

        public DefaultToolContext CreateToolContext()
        {
            return new DefaultToolContext(SendBasicInferenceAsync);
        }

        public async Task<SendInferenceResponse> SendBasicInferenceAsync(SendInferenceRequest req)
        {
            var session = await LocalModel.CreateInteractiveChatSessionAsync();
            var inferenceParams = new InferenceParams()
            {
                MaxTokens = 256,
                TokensKeep = 128,
                SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline()
                {
                    Temperature = req.Temperature > 0 ? req.Temperature : 0.7f,
                    TopP = req.TopP > 0 ? req.TopP : 1.0f
                }
            };

            var resultBuilder = new StringBuilder();
            await foreach (var tok in session.ChatAsync(
                new Message(AuthorRole.User, req.Text), inferenceParams))
            {
                resultBuilder.Append(tok);
            }

            return new SendInferenceResponse { Content = resultBuilder.ToString() };
        }

        private static ServiceProvider BuildServices(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }
    }

    internal class TestSettingsService : ISettingsService
    {
        private readonly string _rootFolder;

        public Settings Settings { get; set; }

        public TestSettingsService(Settings settings, string rootFolder)
        {
            Settings = settings;
            _rootFolder = rootFolder;
        }

        public string GetRootFolder() => _rootFolder;

        public T ParseJson<T>(string json, bool discardUnknownFields = true) where T : IMessage<T>, new()
        {
            var jp = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(discardUnknownFields));
            return jp.Parse<T>(json);
        }

        public Task LoadAsync() => Task.CompletedTask;
        public Task SaveAsync() => Task.CompletedTask;
    }

}
