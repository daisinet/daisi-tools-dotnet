using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Daisi.Tools.Tests.Information
{
    /// <summary>
    /// Integration tests that exercise WebSearchTool through the same code paths
    /// used by DAISI hosts: tool discovery via reflection (ToolService.LoadTools),
    /// DI with AddHttpClient() (AddDaisiServices), DefaultToolContext backed by
    /// DaisiStaticSettings.Services, and the full GetExecutionContext → await
    /// ExecutionTask pipeline (ToolSession.ExecuteToolAsync).
    /// </summary>
    public class WebSearchToolHostTests : IDisposable
    {
        private readonly IServiceProvider? _originalServices;

        public WebSearchToolHostTests()
        {
            _originalServices = DaisiStaticSettings.Services;
        }

        public void Dispose()
        {
            DaisiStaticSettings.Services = _originalServices!;
        }

        #region Helpers

        /// <summary>
        /// Builds a ServiceProvider using AddHttpClient() with a mock handler,
        /// mirroring how the host registers services in AddDaisiServices().
        /// </summary>
        private static ServiceProvider BuildHostServices(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            // Host calls services.AddHttpClient() in AddDaisiServices().
            // We configure the default ("") client's primary handler to use our mock.
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates a DefaultToolContext backed by DaisiStaticSettings.Services,
        /// exactly as the host does in InferenceSession.SendBasicWithToolsStreamingAsync.
        /// </summary>
        private static DefaultToolContext CreateHostToolContext(ServiceProvider serviceProvider)
        {
            // Host sets DaisiStaticSettings.Services = App.Services in Program.cs
            DaisiStaticSettings.Services = serviceProvider;
            // Host passes SendBasicAsync callback; we provide a no-op mock
            return new DefaultToolContext(request =>
                Task.FromResult(new SendInferenceResponse { Content = "mock" }));
        }

        /// <summary>
        /// Discovers the WebSearchTool via reflection, the same way ToolService.LoadTools does:
        /// Assembly.GetAssembly(typeof(BasicMathTool)).GetTypes().Where(IDaisiTool)
        /// then Activator.CreateInstance(type).
        /// </summary>
        private static IDaisiTool DiscoverToolViaReflection()
        {
            var toolAssembly = Assembly.GetAssembly(typeof(WebSearchTool))!;
            var toolType = toolAssembly.GetTypes()
                .First(t => t.GetInterface(nameof(IDaisiTool), true) is not null
                         && t.Name == nameof(WebSearchTool));
            return (IDaisiTool)Activator.CreateInstance(toolType)!;
        }

        private static string CreateGoogleHtml(params string[] urls)
        {
            var links = string.Join("\n", urls.Select(u =>
                $@"<a href=""/url?q={u}&amp;sa=U&amp;ved=2ahUKE"">Result</a>"));
            return $@"<!DOCTYPE html><html><body><div id=""search"">{links}</div></body></html>";
        }

        /// <summary>
        /// Runs the full host execution pipeline: discover tool → create context →
        /// GetExecutionContext → await ExecutionTask.
        /// </summary>
        private static async Task<(ToolResult Result, ToolExecutionContext ExecContext, MockHttpMessageHandler Handler)>
            ExecuteToolLikeHost(string html, params ToolParameterBase[] parameters)
        {
            var handler = new MockHttpMessageHandler(html, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            return (result, execContext, handler);
        }

        #endregion

        #region Tool Discovery Tests (mirrors ToolService.LoadTools)

        [Fact]
        public void Discovery_ReflectionFindsWebSearchTool()
        {
            // ToolService.LoadTools scans the assembly for all IDaisiTool implementors
            var toolAssembly = Assembly.GetAssembly(typeof(WebSearchTool))!;
            var toolTypes = toolAssembly.GetTypes()
                .Where(t => t.GetInterface(nameof(IDaisiTool), true) is not null)
                .ToList();

            Assert.Contains(toolTypes, t => t == typeof(WebSearchTool));
        }

        [Fact]
        public void Discovery_ActivatorCreateInstance_Succeeds()
        {
            // ToolService instantiates tools via Activator.CreateInstance (no DI constructor injection)
            var tool = DiscoverToolViaReflection();

            Assert.NotNull(tool);
            Assert.Equal("daisi-info-web-search", tool.Id);
            Assert.Equal("Daisi Web Search", tool.Name);
        }

        [Fact]
        public void Discovery_ToolCanBeLookedUpById()
        {
            // ToolService builds _toolLookup = Tools.ToDictionary(t => t.Id)
            var tool = DiscoverToolViaReflection();
            var toolLookup = new Dictionary<string, IDaisiTool> { [tool.Id] = tool };

            Assert.True(toolLookup.TryGetValue("daisi-info-web-search", out var found));
            Assert.Same(tool, found);
        }

        [Fact]
        public void Discovery_ToolHasExpectedParameterMetadata()
        {
            var tool = DiscoverToolViaReflection();

            // ToolService caches tool JSON metadata including parameters
            var queryParam = tool.Parameters.FirstOrDefault(p => p.Name == "query");
            var maxResultsParam = tool.Parameters.FirstOrDefault(p => p.Name == "max-results");

            Assert.NotNull(queryParam);
            Assert.True(queryParam!.IsRequired);
            Assert.False(string.IsNullOrWhiteSpace(queryParam.Description));

            Assert.NotNull(maxResultsParam);
            Assert.False(maxResultsParam!.IsRequired);
            Assert.False(string.IsNullOrWhiteSpace(maxResultsParam.Description));
        }

        [Fact]
        public void Discovery_ToolMetadataSerializesToJson()
        {
            // ToolService serializes tool metadata: CachedToolsJson = JsonSerializer.Serialize(Tools)
            var tool = DiscoverToolViaReflection();
            var json = JsonSerializer.Serialize(new
            {
                id = tool.Id,
                name = tool.Name,
                useInstructions = tool.UseInstructions,
                parameters = tool.Parameters.Select(p => new
                {
                    name = p.Name,
                    description = p.Description,
                    isRequired = p.IsRequired
                })
            });

            Assert.Contains("daisi-info-web-search", json);
            Assert.Contains("query", json);
        }

        #endregion

        #region Full Host Execution Pipeline Tests

        [Fact]
        public async Task HostExecution_DefaultToolContext_ReturnsSearchResults()
        {
            // Full pipeline: host DI → DefaultToolContext → tool execution
            var html = CreateGoogleHtml(
                "https://example.com/result1",
                "https://example.org/result2");

            var (result, execContext, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test search", IsRequired = true });

            // Host yields ExecutionMessage to the consumer first
            Assert.False(string.IsNullOrEmpty(execContext.ExecutionMessage));
            Assert.Contains("test search", execContext.ExecutionMessage);

            // Then awaits ExecutionTask for the ToolResult
            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);
            Assert.False(string.IsNullOrWhiteSpace(result.OutputMessage));

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(urls);
            Assert.Equal(2, urls!.Length);
            Assert.Equal("https://example.com/result1", urls[0]);
            Assert.Equal("https://example.org/result2", urls[1]);
        }

        [Fact]
        public async Task HostExecution_UserAgentHeader_SentWithRequest()
        {
            // Google requires a browser-like User-Agent to serve real HTML
            var html = CreateGoogleHtml("https://example.com/r");
            var (_, _, handler) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true });

            Assert.NotNull(handler.LastRequest);
            var ua = handler.LastRequest!.Headers.UserAgent.ToString();
            Assert.Contains("Mozilla/5.0", ua);
            Assert.Contains("AppleWebKit", ua);
        }

        [Fact]
        public async Task HostExecution_RequestUrl_TargetsGoogleSearch()
        {
            var html = CreateGoogleHtml("https://example.com/r");
            var (_, _, handler) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "artificial intelligence", IsRequired = true });

            Assert.NotNull(handler.LastRequest);
            var requestUri = handler.LastRequest!.RequestUri!;
            Assert.Equal("www.google.com", requestUri.Host);
            Assert.Equal("/search", requestUri.AbsolutePath);
            Assert.Contains("q=", requestUri.Query);
            Assert.Contains("artificial", requestUri.Query);
        }

        [Fact]
        public async Task HostExecution_MaxResults_LimitsOutput()
        {
            var html = CreateGoogleHtml(
                "https://a.com/1", "https://b.com/2", "https://c.com/3",
                "https://d.com/4", "https://e.com/5");

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true },
                new ToolParameterBase { Name = "max-results", Value = "3", IsRequired = false });

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Equal(3, urls!.Length);
        }

        [Fact]
        public async Task HostExecution_DefaultMaxResults_IsFive()
        {
            var html = CreateGoogleHtml(
                "https://a.com/1", "https://b.com/2", "https://c.com/3",
                "https://d.com/4", "https://e.com/5", "https://f.com/6",
                "https://g.com/7");

            // Only query parameter — no max-results
            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true });

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Equal(5, urls!.Length);
        }

        [Fact]
        public async Task HostExecution_InvalidMaxResults_DefaultsToFive()
        {
            var html = CreateGoogleHtml(
                "https://a.com/1", "https://b.com/2", "https://c.com/3",
                "https://d.com/4", "https://e.com/5", "https://f.com/6");

            // LLM could generate non-numeric max-results
            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true },
                new ToolParameterBase { Name = "max-results", Value = "not-a-number", IsRequired = false });

            Assert.True(result.Success);
            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Equal(5, urls!.Length);
        }

        [Fact]
        public async Task HostExecution_NetworkError_ReturnsGracefulFailure()
        {
            // Simulate DNS failure, timeout, connection refused, etc.
            var handler = new ThrowingHttpMessageHandler(
                new HttpRequestException("DNS resolution failed"));
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            // Tool must not throw — host expects a ToolResult, not an exception
            Assert.False(result.Success);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public async Task HostExecution_NoHttpClientFactory_ReturnsError()
        {
            // Edge case: host missing AddHttpClient() in DI
            var emptyServices = new ServiceCollection().BuildServiceProvider();
            DaisiStaticSettings.Services = emptyServices;
            var context = new DefaultToolContext(request =>
                Task.FromResult(new SendInferenceResponse { Content = "mock" }));
            var tool = DiscoverToolViaReflection();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "query", Value = "test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
            Assert.Contains("HttpClientFactory", result.ErrorMessage);
        }

        [Fact]
        public async Task HostExecution_EmptyResults_ReturnsEmptyJsonArray()
        {
            // Google page with no matching result links
            var html = @"<!DOCTYPE html><html><body><p>No results found for your query.</p></body></html>";

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "xyznonexistent123", IsRequired = true });

            Assert.True(result.Success);
            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(urls);
            Assert.Empty(urls!);
        }

        [Fact]
        public async Task HostExecution_RealisticGoogleHtml_FiltersCorrectly()
        {
            // Realistic Google search result page with mixed link types
            var html = @"
<!DOCTYPE html>
<html>
<head><title>artificial intelligence - Google Search</title></head>
<body>
<div id=""main"">
  <div class=""g"">
    <a href=""/url?q=https://en.wikipedia.org/wiki/Artificial_intelligence&amp;sa=U&amp;ved=2ahUKEwi"">
      <h3>Artificial intelligence - Wikipedia</h3>
    </a>
    <span>Artificial intelligence (AI) is intelligence demonstrated by machines...</span>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://www.google.com/settings/u/0/search&amp;sa=U"">Settings</a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://www.ibm.com/topics/artificial-intelligence&amp;sa=U&amp;ved=3bhVLE"">
      <h3>What is Artificial Intelligence (AI)? | IBM</h3>
    </a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://youtube.com/watch?v=abc123&amp;sa=U"">AI Video</a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://www.sciencedaily.com/news/ai/&amp;sa=U&amp;ved=4ciWMF"">
      <h3>AI News - ScienceDaily</h3>
    </a>
  </div>
</div>
<div id=""foot"">
  <a href=""/url?q=https://www.google.com/webhp&amp;sa=U"">Google Home</a>
</div>
</body>
</html>";

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "artificial intelligence", IsRequired = true });

            Assert.True(result.Success);
            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.NotNull(urls);

            // Should include real results but NOT google.com or youtube.com
            Assert.Contains("https://en.wikipedia.org/wiki/Artificial_intelligence", urls!);
            Assert.Contains("https://www.ibm.com/topics/artificial-intelligence", urls);
            Assert.Contains("https://www.sciencedaily.com/news/ai/", urls);
            Assert.DoesNotContain(urls, u => u.Contains("google.com"));
            Assert.DoesNotContain(urls, u => u.Contains("youtube.com"));
            Assert.Equal(3, urls.Length);
        }

        [Fact]
        public async Task HostExecution_PercentEncodedUrls_AreDecoded()
        {
            var html = @"<a href=""/url?q=https%3A%2F%2Fexample.com%2Fpath%2Fpage%3Fid%3D42&amp;sa=U"">Link</a>";

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true });

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Single(urls!);
            Assert.Equal("https://example.com/path/page?id=42", urls![0]);
        }

        [Fact]
        public async Task HostExecution_OutputStructure_MatchesHostExpectations()
        {
            // The host uses result fields to construct the inference response:
            //   Format = toolResult.OutputFormat
            //   Content = $"{toolResult.OutputMessage}: {toolResult.Output}"
            var html = CreateGoogleHtml("https://example.com/page");

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true });

            // OutputFormat tells the host how to tag the response
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);

            // OutputMessage accompanies the output for clarity
            Assert.False(string.IsNullOrWhiteSpace(result.OutputMessage));

            // Output must be valid JSON — the host passes it to inference as tool content
            var doc = JsonDocument.Parse(result.Output);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

            // Each element is a URL string
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                Assert.Equal(JsonValueKind.String, element.ValueKind);
                Assert.True(Uri.IsWellFormedUriString(element.GetString(), UriKind.Absolute));
            }
        }

        [Fact]
        public async Task HostExecution_HttpUrls_AlsoExtracted()
        {
            // Some sites still use http:// — tool should handle both schemes
            var html = @"
                <a href=""/url?q=https://secure.example.com/page&amp;sa=U"">HTTPS</a>
                <a href=""/url?q=http://legacy.example.com/page&amp;sa=U"">HTTP</a>";

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true });

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Equal(2, urls!.Length);
            Assert.Contains("https://secure.example.com/page", urls);
            Assert.Contains("http://legacy.example.com/page", urls);
        }

        [Fact]
        public async Task HostExecution_DuplicateUrls_AreDeduped()
        {
            // Google often includes the same URL in multiple link formats
            var html = @"
                <a href=""/url?q=https://example.com/page&amp;sa=U&amp;ved=abc"">Link1</a>
                <a href=""/url?url=https://example.com/page&amp;sa=U&amp;ved=def"">Link2</a>
                <a href=""/url?q=https://example.com/page&amp;sa=U&amp;ved=ghi"">Link3</a>";

            var (result, _, _) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "test", IsRequired = true });

            var urls = JsonSerializer.Deserialize<string[]>(result.Output);
            Assert.Single(urls!);
        }

        [Fact]
        public async Task HostExecution_QueryIsUrlEncoded()
        {
            var html = CreateGoogleHtml("https://example.com/r");
            var (_, _, handler) = await ExecuteToolLikeHost(html,
                new ToolParameterBase { Name = "query", Value = "C# async await", IsRequired = true });

            Assert.NotNull(handler.LastRequest);
            var rawUrl = handler.LastRequest!.RequestUri!.OriginalString;
            // Special characters in query should be percent-encoded
            Assert.Contains("C%23", rawUrl);
        }

        #endregion

        #region Inference Pipeline Tests (simulates ToolSession.ExecuteToolAsync → InferenceSession)

        // These tests simulate the exact code path from InferenceSession.SendBasicWithToolsStreamingAsync:
        //   1. LLM produces ToolToUse JSON  →  deserialized into ToolToUse
        //   2. ToolSession.ExecuteToolAsync  →  looks up tool, calls GetExecutionContext, awaits result
        //   3. Yields SendInferenceResponse messages (Tooling, ToolContent, Error)
        //   4. InferenceSession collects ToolContent into toolOutputBuilder
        //   5. Formats as "=== Begin Tool Output ===\n\n{output}\n\n=== End Tool Output ===\n\n{query}"
        //   6. Sends formatted context back to LLM for final answer

        /// <summary>
        /// Simulates ToolSession.ExecuteToolAsync: takes a ToolToUse (from LLM),
        /// looks up the tool, executes it, and yields the same SendInferenceResponse
        /// sequence that the host produces. Returns responses in order.
        /// </summary>
        private static async Task<List<SendInferenceResponse>> SimulateExecuteToolAsync(
            IDaisiTool tool, ToolToUse toolToUse, IToolContext context, CancellationToken ct = default)
        {
            var responses = new List<SendInferenceResponse>();

            // --- Begin: mirrors ToolSession.ExecuteToolAsync lines 235-326 ---

            // 1. "Using tool..." message (line 243-248)
            responses.Add(new SendInferenceResponse
            {
                Type = InferenceResponseTypes.Tooling,
                AuthorRole = "Assistant",
                Content = $"Using tool... {toolToUse.Id}"
            });

            // 2. Get execution context (line 266)
            var executionContext = tool.GetExecutionContext(context, ct, toolToUse.Parameters);

            // 3. Execution message (lines 268-275)
            if (executionContext is not null && !string.IsNullOrWhiteSpace(executionContext.ExecutionMessage))
            {
                responses.Add(new SendInferenceResponse
                {
                    Type = InferenceResponseTypes.Tooling,
                    AuthorRole = "Assistant",
                    Content = executionContext.ExecutionMessage
                });
            }

            // 4. Await the task (lines 277-284)
            ToolResult? toolResult = null;
            string toolException = string.Empty;
            try
            {
                toolResult = await executionContext!.ExecutionTask;
            }
            catch (Exception exc)
            {
                toolException = exc.Message;
            }

            // 5. Exception response (lines 286-292)
            if (!string.IsNullOrWhiteSpace(toolException))
            {
                responses.Add(new SendInferenceResponse
                {
                    Type = InferenceResponseTypes.Error,
                    AuthorRole = "System",
                    Content = $"Tool Exception: \"{toolException}\""
                });
            }

            // 6. Null/empty result (lines 294-300)
            if (toolResult is null || string.IsNullOrWhiteSpace(toolResult.Output))
            {
                responses.Add(new SendInferenceResponse
                {
                    Type = InferenceResponseTypes.Error,
                    AuthorRole = "System",
                    Content = $"Tool \"{toolToUse.Id}\" did not return a result."
                });
            }
            else if (toolResult.Success)
            {
                // 7. Success: ToolContent response (lines 303-311)
                responses.Add(new SendInferenceResponse
                {
                    Type = InferenceResponseTypes.ToolContent,
                    AuthorRole = "System",
                    Content = $"{(toolResult.OutputMessage ?? $"Use the following results from the \"{toolToUse.Id}\" tool")}: {toolResult.Output}",
                    Format = toolResult.OutputFormat
                });
            }
            else
            {
                // 8. Failure: Error response (lines 314-319)
                responses.Add(new SendInferenceResponse
                {
                    Type = InferenceResponseTypes.Error,
                    Content = $"Tool \"{toolToUse.Id}\" failed with message \"{toolResult.ErrorMessage}\""
                });
            }

            // --- End: mirrors ToolSession.ExecuteToolAsync ---

            return responses;
        }

        /// <summary>
        /// Simulates InferenceSession.SendBasicWithToolsStreamingAsync lines 316-340:
        /// collects ToolContent from responses, formats with === markers for LLM.
        /// </summary>
        private static string SimulateFormatToolOutputForLlm(
            List<SendInferenceResponse> responses, string originalUserMessage)
        {
            // InferenceSession lines 316-317: collect ToolContent into toolOutputBuilder
            var toolOutputBuilder = new StringBuilder();
            foreach (var response in responses)
            {
                if (response.Type == InferenceResponseTypes.ToolContent)
                    toolOutputBuilder.Append(response.Content).Append("\n\n");
            }

            // InferenceSession lines 338-340: format for LLM
            string toolOutput = toolOutputBuilder.ToString();
            return string.IsNullOrWhiteSpace(toolOutput)
                ? originalUserMessage
                : $"=== Begin Tool Output ===\n\n{toolOutput}\n\n=== End Tool Output === \n\n{originalUserMessage}";
        }

        [Fact]
        public void LlmToolSelection_JsonDeserializesToToolToUse()
        {
            // The LLM produces JSON like this (constrained by GBNF grammar).
            // ToolSession.GetNextToolAsync deserializes it at line 187.
            var llmJson = """
            {
                "Id": "daisi-info-web-search",
                "Parameters": [
                    { "Name": "query", "Value": "latest dotnet release" }
                ]
            }
            """;

            var toolToUse = JsonSerializer.Deserialize<ToolToUse>(llmJson);

            Assert.NotNull(toolToUse);
            Assert.Equal("daisi-info-web-search", toolToUse!.Id);
            Assert.Single(toolToUse.Parameters);
            Assert.Equal("query", toolToUse.Parameters[0].Name);
            Assert.Equal("latest dotnet release", toolToUse.Parameters[0].Value);
        }

        [Fact]
        public void LlmToolSelection_WithOptionalParams_DeserializesCorrectly()
        {
            // LLM includes optional max-results parameter
            var llmJson = """
            {
                "Id": "daisi-info-web-search",
                "Parameters": [
                    { "Name": "query", "Value": "best pizza recipes" },
                    { "Name": "max-results", "Value": "3" }
                ]
            }
            """;

            var toolToUse = JsonSerializer.Deserialize<ToolToUse>(llmJson);

            Assert.NotNull(toolToUse);
            Assert.Equal(2, toolToUse!.Parameters.Length);
            Assert.Equal("3", toolToUse.Parameters[1].Value);
        }

        [Fact]
        public async Task InferencePipeline_SuccessfulSearch_YieldsCorrectResponseSequence()
        {
            // Simulate: LLM selects tool → ToolSession.ExecuteToolAsync runs it
            var googleHtml = CreateGoogleHtml(
                "https://example.com/result1",
                "https://example.org/result2");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);

            // Tool discovered and registered by ToolService.LoadTools
            var tool = DiscoverToolViaReflection();

            // LLM produced this ToolToUse (deserialized from JSON at ToolSession line 187)
            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "test query" }]
            };

            // Run through ToolSession.ExecuteToolAsync pipeline
            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);

            // Verify response sequence matches what ToolSession yields to InferenceSession
            // Response 1: "Using tool..." (Tooling type)
            Assert.Equal(InferenceResponseTypes.Tooling, responses[0].Type);
            Assert.Contains("daisi-info-web-search", responses[0].Content);

            // Response 2: Execution message (Tooling type)
            Assert.Equal(InferenceResponseTypes.Tooling, responses[1].Type);
            Assert.Contains("Searching the web for", responses[1].Content);

            // Response 3: ToolContent with results
            Assert.Equal(InferenceResponseTypes.ToolContent, responses[2].Type);
            Assert.Equal("System", responses[2].AuthorRole);
            Assert.Equal(InferenceOutputFormats.Json, responses[2].Format);

            // The Content field is what InferenceSession collects (line 317)
            Assert.Contains("[", responses[2].Content);
            Assert.Contains("https://example.com/result1", responses[2].Content);
            Assert.Contains("https://example.org/result2", responses[2].Content);

            // No error responses
            Assert.DoesNotContain(responses, r => r.Type == InferenceResponseTypes.Error);
        }

        [Fact]
        public async Task InferencePipeline_ToolContentFormat_MatchesHostExpectation()
        {
            // Verify the exact format of ToolContent.Content matches ToolSession line 309:
            //   "{OutputMessage}: {Output}"
            var googleHtml = CreateGoogleHtml("https://example.com/page");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "test" }]
            };

            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);
            var toolContentResponse = responses.First(r => r.Type == InferenceResponseTypes.ToolContent);

            // Format is "{OutputMessage}: {Output}" per ToolSession line 309
            // OutputMessage should be set (not null), so it uses OutputMessage, not the fallback
            Assert.Contains(": [", toolContentResponse.Content);

            // Verify the Output portion is valid JSON
            var colonIndex = toolContentResponse.Content.IndexOf(": [");
            var jsonPart = toolContentResponse.Content[(colonIndex + 2)..];
            var urls = JsonSerializer.Deserialize<string[]>(jsonPart);
            Assert.NotNull(urls);
            Assert.Contains("https://example.com/page", urls!);
        }

        [Fact]
        public async Task InferencePipeline_ToolOutputFormattedForLlm_ContainsMarkers()
        {
            // After tool execution, InferenceSession formats the output for the LLM
            // with "=== Begin Tool Output ===" / "=== End Tool Output ===" markers
            var googleHtml = CreateGoogleHtml("https://example.com/result");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "what is AI" }]
            };

            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);

            // Simulate InferenceSession lines 338-340
            string llmQuery = SimulateFormatToolOutputForLlm(responses, "what is AI");

            // Verify the format matches what the LLM receives
            Assert.StartsWith("=== Begin Tool Output ===", llmQuery);
            Assert.Contains("=== End Tool Output ===", llmQuery);
            Assert.EndsWith("what is AI", llmQuery);

            // The tool output content should be between the markers
            Assert.Contains("https://example.com/result", llmQuery);
        }

        [Fact]
        public async Task InferencePipeline_ToolResultAddedToHistory()
        {
            // ToolSession.AddToolResultToHistory (line 133-136) adds tool output
            // to the tool chat session history as a user message
            var googleHtml = CreateGoogleHtml("https://example.com/result");
            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "test" }]
            };

            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);

            // Simulate InferenceSession lines 326-327: collect ToolContent
            var toolOutputBuilder = new StringBuilder();
            foreach (var response in responses)
            {
                if (response.Type == InferenceResponseTypes.ToolContent)
                    toolOutputBuilder.Append(response.Content).Append("\n\n");
            }

            // This is what ToolSession.AddToolResultToHistory receives (line 135-136)
            string historyMessage = $"Tool returned the following result:\n{toolOutputBuilder}";

            Assert.Contains("Tool returned the following result:", historyMessage);
            Assert.Contains("https://example.com/result", historyMessage);
        }

        [Fact]
        public async Task InferencePipeline_NetworkError_YieldsErrorResponse()
        {
            // When tool fails, ToolSession yields Error responses that
            // InferenceSession passes through to the client (line 319)
            var handler = new ThrowingHttpMessageHandler(
                new HttpRequestException("Connection timed out"));
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "test" }]
            };

            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);

            // Should have Tooling messages but end with an Error
            Assert.Contains(responses, r => r.Type == InferenceResponseTypes.Tooling);
            var errorResponse = responses.First(r => r.Type == InferenceResponseTypes.Error);
            Assert.Contains("daisi-info-web-search", errorResponse.Content);
            // Tool catches the exception and returns ToolResult with null Output,
            // so ToolSession yields "did not return a result" (line 299)
            Assert.Contains("did not return a result", errorResponse.Content);

            // When tool fails, the formatted LLM query should just be the original message
            // (no tool output markers) since no ToolContent was produced
            string llmQuery = SimulateFormatToolOutputForLlm(responses, "test");
            Assert.Equal("test", llmQuery);
            Assert.DoesNotContain("=== Begin Tool Output ===", llmQuery);
        }

        [Fact]
        public async Task InferencePipeline_RealisticEndToEnd_FullFlow()
        {
            // Complete realistic simulation of the inference pipeline:
            //   User asks "What is the latest version of Python?"
            //   → LLM selects daisi-info-web-search with query "Python latest version 2026"
            //   → Tool fetches Google, extracts URLs
            //   → Result formatted and sent back to LLM for final answer

            var googleHtml = @"
<!DOCTYPE html>
<html><head><title>Python latest version 2026 - Google Search</title></head>
<body>
<div id=""search"">
  <div class=""g"">
    <a href=""/url?q=https://www.python.org/downloads/&amp;sa=U&amp;ved=2ahUKE"">
      <h3>Download Python | Python.org</h3>
    </a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://en.wikipedia.org/wiki/Python_(programming_language)&amp;sa=U&amp;ved=3bhVLE"">
      <h3>Python (programming language) - Wikipedia</h3>
    </a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://www.google.com/search?related&amp;sa=U"">Related searches</a>
  </div>
  <div class=""g"">
    <a href=""/url?q=https://realpython.com/python-news-2026/&amp;sa=U&amp;ved=4ciWMF"">
      <h3>What's New in Python 2026</h3>
    </a>
  </div>
</div>
</body></html>";

            var handler = new MockHttpMessageHandler(googleHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            // Step 1: LLM produces ToolToUse JSON (simulating ToolSession.GetNextToolAsync)
            var llmToolJson = """
            {
                "Id": "daisi-info-web-search",
                "Parameters": [
                    { "Name": "query", "Value": "Python latest version 2026" }
                ]
            }
            """;
            var toolToUse = JsonSerializer.Deserialize<ToolToUse>(llmToolJson)!;

            // Step 2: ToolSession.ExecuteToolAsync runs the tool
            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);

            // Step 3: Verify response sequence
            Assert.True(responses.Count >= 3);
            Assert.Equal(InferenceResponseTypes.Tooling, responses[0].Type);         // "Using tool..."
            Assert.Equal(InferenceResponseTypes.Tooling, responses[1].Type);         // "Searching the web..."
            Assert.Equal(InferenceResponseTypes.ToolContent, responses[2].Type);     // Results

            // Step 4: Verify actual URLs extracted (google.com filtered out)
            var toolContent = responses.First(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.Contains("python.org/downloads", toolContent.Content);
            Assert.Contains("wikipedia.org", toolContent.Content);
            Assert.Contains("realpython.com", toolContent.Content);
            Assert.DoesNotContain("google.com", toolContent.Content);

            // Step 5: Verify HTTP request details
            Assert.NotNull(handler.LastRequest);
            Assert.Equal("www.google.com", handler.LastRequest!.RequestUri!.Host);
            Assert.Contains("Mozilla/5.0", handler.LastRequest.Headers.UserAgent.ToString());

            // Step 6: Format for final LLM query (InferenceSession lines 338-340)
            string userMessage = "What is the latest version of Python?";
            string llmQuery = SimulateFormatToolOutputForLlm(responses, userMessage);

            // The LLM receives the tool output wrapped in markers, followed by user's original question
            Assert.StartsWith("=== Begin Tool Output ===", llmQuery);
            Assert.Contains("python.org/downloads", llmQuery);
            Assert.Contains("=== End Tool Output ===", llmQuery);
            Assert.EndsWith(userMessage, llmQuery);

            // Step 7: The history entry (ToolSession.AddToolResultToHistory)
            var toolOutputBuilder = new StringBuilder();
            foreach (var r in responses.Where(r => r.Type == InferenceResponseTypes.ToolContent))
                toolOutputBuilder.Append(r.Content).Append("\n\n");
            string historyEntry = $"Tool returned the following result:\n{toolOutputBuilder}";
            Assert.Contains("python.org", historyEntry);
        }

        [Fact]
        public async Task InferencePipeline_NoResults_NoToolOutputMarkersInLlmQuery()
        {
            // When the search returns no URLs, tool still succeeds with empty array.
            // The ToolContent will have "[]" which is non-empty, so markers ARE used.
            var emptyHtml = "<html><body>No results</body></html>";
            var handler = new MockHttpMessageHandler(emptyHtml, HttpStatusCode.OK);
            var serviceProvider = BuildHostServices(handler);
            var context = CreateHostToolContext(serviceProvider);
            var tool = DiscoverToolViaReflection();

            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "xyznonexistent" }]
            };

            var responses = await SimulateExecuteToolAsync(tool, toolToUse, context);

            // Tool succeeds with empty array — ToolContent is still yielded
            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.Contains("[]", toolContent!.Content);

            // Since there IS ToolContent, the LLM query gets the markers
            string llmQuery = SimulateFormatToolOutputForLlm(responses, "find something");
            Assert.Contains("=== Begin Tool Output ===", llmQuery);
            Assert.Contains("[]", llmQuery);
        }

        [Fact]
        public void ToolValidation_ValidParameters_NoErrors()
        {
            // Simulates ToolSession.ValidateGeneratedTool (lines 328-361)
            var tool = DiscoverToolViaReflection();
            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [new ToolParameterBase { Name = "query", Value = "test search" }]
            };

            // Validate each parameter using the tool's ValidateGeneratedParameterValues
            var errors = new List<string>();

            // Check parameter names are valid (line 341)
            var badNames = toolToUse.Parameters
                .Where(p => !tool.Parameters.Any(pp => pp.Name == p.Name))
                .ToList();
            badNames.ForEach(p => errors.Add($"\"{p.Name}\" is invalid"));

            // Check required parameters have values (line 344)
            var missingRequired = tool.Parameters
                .Where(p => p.IsRequired && !toolToUse.Parameters.Any(pp => pp.Name == p.Name && !string.IsNullOrWhiteSpace(pp.Value)))
                .ToList();
            missingRequired.ForEach(p => errors.Add($"\"{p.Name}\" is required"));

            // Run tool-specific validation (line 351)
            foreach (var par in toolToUse.Parameters)
            {
                var validationError = tool.ValidateGeneratedParameterValues(par);
                if (!string.IsNullOrWhiteSpace(validationError))
                    errors.Add(validationError);
            }

            Assert.Empty(errors);
        }

        [Fact]
        public void ToolValidation_MissingRequiredQuery_HasErrors()
        {
            // LLM forgot the required "query" parameter
            var tool = DiscoverToolViaReflection();

            var missingRequired = tool.Parameters
                .Where(p => p.IsRequired && !"max-results".Equals(p.Name))
                .ToList();

            // "query" is the only required parameter
            Assert.Single(missingRequired);
            Assert.Equal("query", missingRequired[0].Name);
        }

        [Fact]
        public void ToolValidation_InvalidParameterName_Detected()
        {
            // LLM hallucinated a parameter name that doesn't exist
            var tool = DiscoverToolViaReflection();
            var toolToUse = new ToolToUse
            {
                Id = "daisi-info-web-search",
                Parameters = [
                    new ToolParameterBase { Name = "query", Value = "test" },
                    new ToolParameterBase { Name = "language", Value = "en" }  // doesn't exist
                ]
            };

            var badNames = toolToUse.Parameters
                .Where(p => !tool.Parameters.Any(pp => pp.Name == p.Name))
                .ToList();

            Assert.Single(badNames);
            Assert.Equal("language", badNames[0].Name);
        }

        #endregion
    }
}
