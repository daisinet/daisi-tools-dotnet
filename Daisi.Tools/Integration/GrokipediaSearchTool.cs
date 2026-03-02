using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Daisi.Tools.Integration
{
    public class GrokipediaSearchTool : DaisiToolBase
    {
        private const string P_QUERY = "query";
        private const string P_MAX_RESULTS = "max-results";
        internal const string GrokipediaApiBaseUrl = "https://grokipedia.com/api/full-text-search";

        public override string Id => "daisi-integration-grokipedia";
        public override string Name => "Daisi Grokipedia Search";

        public override string UseInstructions =>
            "Use this tool to search Grokipedia encyclopedia articles for factual and encyclopedic information. " +
            "Returns article titles, snippets, and URLs from Grokipedia.com. " +
            "Keywords: grokipedia, encyclopedia, wiki, factual lookup, who is, what is, biography, history.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_QUERY, Description = "The search query to look up on Grokipedia.", IsRequired = true },
            new ToolParameter() { Name = P_MAX_RESULTS, Description = "Maximum number of results to return. Default is 3.", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var query = parameters.GetParameterValueOrDefault(P_QUERY);
            var maxResultsStr = parameters.GetParameterValueOrDefault(P_MAX_RESULTS, "3");
            if (!int.TryParse(maxResultsStr, out var maxResults))
                maxResults = 3;

            return new ToolExecutionContext
            {
                ExecutionMessage = $"Searching Grokipedia for: {query}",
                ExecutionTask = SearchGrokipedia(toolContext, query, maxResults, cancellation)
            };
        }

        private async Task<ToolResult> SearchGrokipedia(IToolContext toolContext, string query, int maxResults, CancellationToken ct)
        {
            try
            {
                var httpClientFactory = toolContext.Services.GetService<IHttpClientFactory>();
                if (httpClientFactory is null)
                    return new ToolResult { Success = false, ErrorMessage = "HttpClientFactory is not available in the current context." };

                using var client = httpClientFactory.CreateClient();

                var url = $"{GrokipediaApiBaseUrl}?query={Uri.EscapeDataString(query)}" +
                          $"&limit={maxResults}&offset=0";

                var json = await client.GetStringAsync(url, ct);
                var results = ParseResults(json);

                return new ToolResult
                {
                    Output = JsonSerializer.Serialize(results),
                    OutputMessage = $"Found {results.Length} Grokipedia results",
                    OutputFormat = InferenceOutputFormats.Json,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ToolResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        internal static GrokipediaResult[] ParseResults(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var searchArray = doc.RootElement.GetProperty("results");

            var results = new List<GrokipediaResult>();
            foreach (var item in searchArray.EnumerateArray())
            {
                var title = item.GetProperty("title").GetString() ?? "";
                var snippet = item.GetProperty("snippet").GetString() ?? "";
                var slug = item.GetProperty("slug").GetString() ?? "";
                // Strip HTML tags from snippet
                snippet = System.Text.RegularExpressions.Regex.Replace(snippet, @"<[^>]+>", "");

                results.Add(new GrokipediaResult
                {
                    Title = title,
                    Snippet = snippet,
                    Url = $"https://grokipedia.com/page/{Uri.EscapeDataString(slug)}"
                });
            }

            return results.ToArray();
        }

        internal class GrokipediaResult
        {
            public string Title { get; set; } = "";
            public string Snippet { get; set; } = "";
            public string Url { get; set; } = "";
        }
    }
}
