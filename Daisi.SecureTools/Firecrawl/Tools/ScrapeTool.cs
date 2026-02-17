using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Firecrawl.Tools;

/// <summary>
/// Scrape a single web page and return its content as markdown.
/// </summary>
public class ScrapeTool(FirecrawlClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string apiKey, string baseUrl, List<ParameterValue> parameters)
    {
        var url = parameters.FirstOrDefault(p => p.Name == "url")?.Value;
        if (string.IsNullOrEmpty(url))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'url' parameter is required." };

        var formatsParam = parameters.FirstOrDefault(p => p.Name == "formats")?.Value;
        var formats = new[] { "markdown" };
        if (!string.IsNullOrEmpty(formatsParam))
        {
            try { formats = JsonSerializer.Deserialize<string[]>(formatsParam) ?? formats; }
            catch { /* use default */ }
        }

        var body = new { url, formats };
        var result = await client.PostAsync(apiKey, baseUrl, "/v1/scrape", body);

        var output = result.TryGetProperty("data", out var data)
            ? data.ToString()
            : result.ToString();

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "markdown",
            OutputMessage = $"Scraped {url}"
        };
    }
}
