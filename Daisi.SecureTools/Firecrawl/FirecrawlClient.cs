using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Daisi.SecureTools.Firecrawl;

/// <summary>
/// HTTP client wrapper for the Firecrawl REST API.
/// </summary>
public class FirecrawlClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public FirecrawlClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Send a POST request to the Firecrawl API.
    /// </summary>
    public async Task<JsonElement> PostAsync(string apiKey, string baseUrl, string endpoint, object? body = null)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new FirecrawlException($"Firecrawl API error ({response.StatusCode}): {responseBody}");

        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
    }

    /// <summary>
    /// Send a GET request to the Firecrawl API.
    /// </summary>
    public async Task<JsonElement> GetAsync(string apiKey, string baseUrl, string endpoint)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new FirecrawlException($"Firecrawl API error ({response.StatusCode}): {responseBody}");

        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
    }
}

public class FirecrawlException : Exception
{
    public FirecrawlException(string message) : base(message) { }
}
