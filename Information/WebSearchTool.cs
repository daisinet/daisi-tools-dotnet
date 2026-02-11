using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Daisi.Tools.Information
{
    public class WebSearchTool : DaisiToolBase
    {
        private const string P_QUERY = "query";
        private const string P_MAX_RESULTS = "max-results";
        internal const string GoogleSearchBaseUrl = "https://www.google.com/search";

        public override string Id => "daisi-info-web-search";
        public override string Name => "Daisi Web Search";

        public override string UseInstructions =>
            "Use this tool for general web search using a search engine. Takes a search query and returns relevant URLs. " +
            "Keywords: search the web, google, find online, look up, find information, current news, recent data, breakthroughs, discover. " +
            "Do NOT use for fetching a specific known URL — use daisi-web-clients-http-get for that. " +
            "Only use daisi-integration-wikipedia when the user explicitly mentions Wikipedia or wiki.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_QUERY,
                Description = "The search query to use for web search.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_MAX_RESULTS,
                Description = "The maximum number of results to return. Default is 5.",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var pQuery = parameters.GetParameter(P_QUERY);
            var query = pQuery.Value;

            var maxResultsStr = parameters.GetParameterValueOrDefault(P_MAX_RESULTS, "5");
            if (!int.TryParse(maxResultsStr, out var maxResults))
                maxResults = 5;

            Task<ToolResult> task = SearchWeb(toolContext, query, maxResults, cancellation);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Searching the web for: {query}"
            };
        }

        private async Task<ToolResult> SearchWeb(IToolContext toolContext, string query, int maxResults, CancellationToken cancellationToken)
        {
            try
            {
                IHttpClientFactory? httpClientFactory = toolContext.Services.GetService<IHttpClientFactory>();
                if (httpClientFactory is null)
                {
                    return new ToolResult()
                    {
                        Success = false,
                        ErrorMessage = "HttpClientFactory is not available in the current context."
                    };
                }

                using var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var searchUrl = $"{GoogleSearchBaseUrl}?q={Uri.EscapeDataString(query)}";
                var html = await client.GetStringAsync(searchUrl, cancellationToken);

                var urls = ExtractUrls(html, maxResults);

                var toolResult = new ToolResult();
                toolResult.OutputFormat = InferenceOutputFormats.Json;
                toolResult.Output = JsonSerializer.Serialize(urls);
                toolResult.OutputMessage = $"Found {urls.Length} search results";
                toolResult.Success = true;

                return toolResult;
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Extracts URLs from Google search HTML.
        /// Handles both url= and q= parameter patterns, and both literal (https://) and percent-encoded (https%3A%2F%2F) schemes.
        /// </summary>
        internal static string[] ExtractUrls(string html, int maxResults)
        {
            // Match both url= and q= parameter patterns (Google uses both)
            // Handle both literal (https://) and percent-encoded (https%3A%2F%2F) schemes
            var pattern = @"(?:url|q)=(https?(?:%3A%2F%2F|://)(?!(?:[^&]*\.)?(?:google|youtube)\.[^&]*)[^&\s""<>]+)";
            var matches = Regex.Matches(html, pattern);

            var urls = matches
                .Select(m => Uri.UnescapeDataString(m.Groups[1].Value))
                .Distinct()
                .Take(maxResults)
                .ToArray();

            return urls;
        }
    }
}
