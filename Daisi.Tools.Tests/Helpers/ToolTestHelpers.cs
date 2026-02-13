using Daisi.Protos.V1;
using Daisi.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Daisi.Tools.Tests.Helpers
{
    public static class ToolTestHelpers
    {
        public static MockToolContext BuildContextWithMockHttp(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory>(new MockHttpClientFactory(handler));
            var provider = services.BuildServiceProvider();
            return new MockToolContext(services: provider);
        }

        public static MockToolContext BuildContextWithMockHttp(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handler = new MockHttpMessageHandler(responseContent, statusCode);
            return BuildContextWithMockHttp(handler);
        }

        /// <summary>
        /// Builds a ServiceProvider with mock HTTP for use in fixture-based tests
        /// that need DaisiStaticSettings.Services configured.
        /// </summary>
        public static ServiceProvider BuildServicesForFixture(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates a temp directory for file tool tests. Caller must delete when done.
        /// </summary>
        public static string CreateTempTestDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "daisi-test-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Asserts that the tool session selected the expected tool AND that executing it
        /// produces a ToolContent response without errors.
        /// Returns the ToolContent response content string.
        /// </summary>
        public static async Task<string> AssertToolSelectedAndExecuted(
            ToolInferenceFixture fixture,
            Daisi.Host.Core.Services.Models.ToolSession toolSession,
            string expectedToolId)
        {
            Assert.True(toolSession.CurrentTool is not null,
                $"LLM returned null CurrentTool. Expected tool: {expectedToolId}");
            Assert.True(toolSession.CurrentTool!.Id == expectedToolId,
                $"Expected tool '{expectedToolId}' but got '{toolSession.CurrentTool.Id}'");

            var context = fixture.CreateToolContext();
            var responses = new List<SendInferenceResponse>();
            await foreach (var response in toolSession.ExecuteToolAsync(context))
            {
                if (response is not null)
                    responses.Add(response);
            }

            // Should have Tooling responses
            Assert.Contains(responses, r => r.Type == InferenceResponseTypes.Tooling);

            // Should NOT have Error responses
            var errors = responses.Where(r => r.Type == InferenceResponseTypes.Error).ToList();
            Assert.True(errors.Count == 0,
                $"Tool '{expectedToolId}' produced errors: {string.Join("; ", errors.Select(e => e.Content))}");

            // Should have ToolContent
            var toolContent = responses.FirstOrDefault(r => r.Type == InferenceResponseTypes.ToolContent);
            Assert.NotNull(toolContent);
            Assert.False(string.IsNullOrWhiteSpace(toolContent!.Content),
                $"Tool '{expectedToolId}' returned empty ToolContent");

            return toolContent.Content;
        }

        public static string CreateMockGoogleHtml(params string[] urls)
        {
            var links = string.Join("\n", urls.Select(u =>
                $@"<a href=""/url?q={u}&amp;sa=U&amp;ved=abc"">Result</a>"));

            return $@"
<!DOCTYPE html>
<html><body>
<div id=""search"">
{links}
</div>
</body></html>";
        }

        public static string CreateMockWikipediaResponse(params (string Title, string Snippet)[] results)
        {
            var searchItems = string.Join(",", results.Select(r =>
                $@"{{""title"":""{EscapeJson(r.Title)}"",""snippet"":""{EscapeJson(r.Snippet)}"",""pageid"":1}}"));

            return $@"{{""query"":{{""search"":[{searchItems}]}}}}";
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
