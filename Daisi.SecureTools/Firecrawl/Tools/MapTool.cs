using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Firecrawl.Tools;

/// <summary>
/// Discover all URLs on a website by mapping its structure.
/// </summary>
public class MapTool(FirecrawlClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string apiKey, string baseUrl, List<ParameterValue> parameters)
    {
        var url = parameters.FirstOrDefault(p => p.Name == "url")?.Value;
        if (string.IsNullOrEmpty(url))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'url' parameter is required." };

        var body = new { url };
        var result = await client.PostAsync(apiKey, baseUrl, "/v1/map", body);

        var output = result.TryGetProperty("links", out var links)
            ? links.ToString()
            : result.ToString();

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Mapped URLs from {url}"
        };
    }
}
