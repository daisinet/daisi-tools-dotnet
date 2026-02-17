using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Firecrawl.Tools;

/// <summary>
/// Crawl multiple pages from a website domain.
/// </summary>
public class CrawlTool(FirecrawlClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string apiKey, string baseUrl, List<ParameterValue> parameters)
    {
        var url = parameters.FirstOrDefault(p => p.Name == "url")?.Value;
        if (string.IsNullOrEmpty(url))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'url' parameter is required." };

        var maxPagesStr = parameters.FirstOrDefault(p => p.Name == "maxPages")?.Value;
        var includesParam = parameters.FirstOrDefault(p => p.Name == "includes")?.Value;
        var excludesParam = parameters.FirstOrDefault(p => p.Name == "excludes")?.Value;

        var body = new Dictionary<string, object> { ["url"] = url };

        if (int.TryParse(maxPagesStr, out var maxPages))
            body["limit"] = maxPages;

        if (!string.IsNullOrEmpty(includesParam))
            body["includePaths"] = includesParam.Split(',', StringSplitOptions.TrimEntries);

        if (!string.IsNullOrEmpty(excludesParam))
            body["excludePaths"] = excludesParam.Split(',', StringSplitOptions.TrimEntries);

        var result = await client.PostAsync(apiKey, baseUrl, "/v1/crawl", body);

        // Crawl returns an async job - return the job ID and status
        var output = result.ToString();

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Started crawl of {url}"
        };
    }
}
