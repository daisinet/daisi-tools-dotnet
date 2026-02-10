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

        public override string Id => "daisi-info-web-search";
        public override string Name => "Daisi Web Search";

        public override string UseInstructions =>
            "Use this tool to search the web for information. " +
            "Provide a search query and optionally the maximum number of result URLs to return.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_QUERY,
                Description = "The search query to send to Google.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_MAX_RESULTS,
                Description = "The maximum number of result URLs to return. Default is 5.",
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

                var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
                var html = await client.GetStringAsync(searchUrl, cancellationToken);

                var urls = ExtractUrls(html, maxResults);

                var result = new ToolResult();
                result.OutputFormat = InferenceOutputFormats.Json;
                result.Output = JsonSerializer.Serialize(urls);
                result.OutputMessage = $"Found {urls.Length} search result URLs";
                result.Success = true;

                return result;
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }

        internal static string[] ExtractUrls(string html, int maxResults)
        {
            var pattern = @"url=((?!.*(google|youtube))([http|https]\S*))&amp;";
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
