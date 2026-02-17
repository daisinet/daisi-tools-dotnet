using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace SecureToolProvider.Tests.Helpers;

public static class TestHelpers
{
    /// <summary>
    /// Create a mock GET request with query parameters.
    /// </summary>
    public static MockHttpRequestData CreateGetRequest(string url)
    {
        var context = new MockFunctionContext();
        return new MockHttpRequestData(context, new Uri(url));
    }

    /// <summary>
    /// Create a mock POST request with a JSON body.
    /// </summary>
    public static MockHttpRequestData CreatePostRequest<T>(string url, T body)
    {
        var context = new MockFunctionContext();
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new MockHttpRequestData(context, new Uri(url), stream);
    }

    /// <summary>
    /// Read the JSON response body from a mock response.
    /// </summary>
    public static async Task<T?> ReadResponseAsync<T>(HttpResponseData response)
    {
        response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <summary>
    /// Read the response body as string.
    /// </summary>
    public static async Task<string> ReadResponseStringAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
}
