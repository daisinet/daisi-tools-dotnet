using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Firecrawl.Tools;

/// <summary>
/// AI-powered structured data extraction from a web page.
/// </summary>
public class ExtractTool(FirecrawlClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string apiKey, string baseUrl, List<ParameterValue> parameters)
    {
        var url = parameters.FirstOrDefault(p => p.Name == "url")?.Value;
        if (string.IsNullOrEmpty(url))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'url' parameter is required." };

        var prompt = parameters.FirstOrDefault(p => p.Name == "prompt")?.Value;
        if (string.IsNullOrEmpty(prompt))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'prompt' parameter is required." };

        var schemaParam = parameters.FirstOrDefault(p => p.Name == "schema")?.Value;

        var body = new Dictionary<string, object>
        {
            ["urls"] = new[] { url },
            ["prompt"] = prompt
        };

        if (!string.IsNullOrEmpty(schemaParam))
        {
            try
            {
                var schema = JsonSerializer.Deserialize<JsonElement>(schemaParam);
                body["schema"] = schema;
            }
            catch { /* ignore invalid schema */ }
        }

        var result = await client.PostAsync(apiKey, baseUrl, "/v1/extract", body);

        var output = result.TryGetProperty("data", out var data)
            ? data.ToString()
            : result.ToString();

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Extracted data from {url}"
        };
    }
}
