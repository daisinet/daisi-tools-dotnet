using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Daisi.SecureTools.Social;

/// <summary>
/// Shared HTTP client wrapper for social media platform API calls.
/// </summary>
public class SocialHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SocialHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Send a JSON POST request with Bearer token authentication.
    /// </summary>
    public async Task<JsonElement> PostJsonAsync(string url, string bearerToken, object? body = null,
        Dictionary<string, string>? extraHeaders = null)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        if (extraHeaders is not null)
        {
            foreach (var (key, value) in extraHeaders)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new SocialApiException($"API error ({response.StatusCode}): {responseBody}");

        if (string.IsNullOrWhiteSpace(responseBody))
            return JsonSerializer.Deserialize<JsonElement>("{}");

        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
    }

    /// <summary>
    /// Send a multipart form POST request with Bearer token authentication.
    /// </summary>
    public async Task<JsonElement> PostFormAsync(string url, string bearerToken,
        MultipartFormDataContent formData, Dictionary<string, string>? extraHeaders = null)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = formData;

        if (extraHeaders is not null)
        {
            foreach (var (key, value) in extraHeaders)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new SocialApiException($"API error ({response.StatusCode}): {responseBody}");

        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
    }

    /// <summary>
    /// Send a form-urlencoded POST request with optional Basic or Bearer auth.
    /// </summary>
    public async Task<JsonElement> PostFormUrlEncodedAsync(string url,
        Dictionary<string, string> formFields, string? bearerToken = null,
        string? basicAuth = null, Dictionary<string, string>? extraHeaders = null)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        if (bearerToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        else if (basicAuth is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        if (extraHeaders is not null)
        {
            foreach (var (key, value) in extraHeaders)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        request.Content = new FormUrlEncodedContent(formFields);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new SocialApiException($"API error ({response.StatusCode}): {responseBody}");

        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
    }

    /// <summary>
    /// Send a raw binary PUT request (for LinkedIn/TikTok media uploads).
    /// </summary>
    public async Task PutBinaryAsync(string url, byte[] data, string contentType,
        Dictionary<string, string>? extraHeaders = null)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, url);

        if (extraHeaders is not null)
        {
            foreach (var (key, value) in extraHeaders)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        request.Content = new ByteArrayContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new SocialApiException($"Upload error ({response.StatusCode}): {responseBody}");
        }
    }

    /// <summary>
    /// Send a JSON GET request with Bearer token authentication.
    /// </summary>
    public async Task<JsonElement> GetJsonAsync(string url, string bearerToken,
        Dictionary<string, string>? extraHeaders = null)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        if (extraHeaders is not null)
        {
            foreach (var (key, value) in extraHeaders)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new SocialApiException($"API error ({response.StatusCode}): {responseBody}");

        return JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
    }
}

public class SocialApiException : Exception
{
    public SocialApiException(string message) : base(message) { }
}
