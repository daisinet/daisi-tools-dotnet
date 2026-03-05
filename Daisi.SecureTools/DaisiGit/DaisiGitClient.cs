using System.Net.Http.Json;
using System.Text.Json;

namespace Daisi.SecureTools.DaisiGit;

/// <summary>
/// HTTP client for calling the DaisiGit REST API from secure tool executors.
/// </summary>
public class DaisiGitClient(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// GET request to the DaisiGit API.
    /// </summary>
    public async Task<JsonElement> GetAsync(string baseUrl, string sessionId, string path)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("X-Session-Id", sessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// POST request to the DaisiGit API.
    /// </summary>
    public async Task<JsonElement> PostAsync(string baseUrl, string sessionId, string path, object body)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("X-Session-Id", sessionId);
        request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// PATCH request to the DaisiGit API.
    /// </summary>
    public async Task<JsonElement> PatchAsync(string baseUrl, string sessionId, string path, object body)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("X-Session-Id", sessionId);
        request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// PUT request to the DaisiGit API.
    /// </summary>
    public async Task PutAsync(string baseUrl, string sessionId, string path)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("X-Session-Id", sessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// DELETE request to the DaisiGit API.
    /// </summary>
    public async Task DeleteAsync(string baseUrl, string sessionId, string path)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("X-Session-Id", sessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
