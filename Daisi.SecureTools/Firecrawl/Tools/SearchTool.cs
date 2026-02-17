using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Firecrawl.Tools;

/// <summary>
/// Search the web and extract content from results.
/// </summary>
public class SearchTool(FirecrawlClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string apiKey, string baseUrl, List<ParameterValue> parameters)
    {
        var query = parameters.FirstOrDefault(p => p.Name == "query")?.Value;
        if (string.IsNullOrEmpty(query))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'query' parameter is required." };

        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;

        var body = new Dictionary<string, object> { ["query"] = query };

        if (int.TryParse(maxResultsStr, out var maxResults))
            body["limit"] = maxResults;

        var result = await client.PostAsync(apiKey, baseUrl, "/v1/search", body);

        var output = result.TryGetProperty("data", out var data)
            ? data.ToString()
            : result.ToString();

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Search results for: {query}"
        };
    }
}
