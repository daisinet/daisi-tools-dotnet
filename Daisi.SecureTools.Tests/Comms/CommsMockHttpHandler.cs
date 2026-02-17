using System.Net;

namespace Daisi.SecureTools.Tests.Comms;

/// <summary>
/// Mock HTTP message handler for testing communications API calls.
/// Supports queuing multiple responses for multi-step API flows.
/// </summary>
public class CommsMockHttpHandler : HttpMessageHandler
{
    private readonly Queue<(string Content, HttpStatusCode StatusCode)> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string?> RequestBodies { get; } = [];

    public HttpRequestMessage? LastRequest => Requests.Count > 0 ? Requests[^1] : null;
    public string? LastRequestBody => RequestBodies.Count > 0 ? RequestBodies[^1] : null;

    public CommsMockHttpHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses.Enqueue((responseContent, statusCode));
    }

    public CommsMockHttpHandler(params (string Content, HttpStatusCode StatusCode)[] responses)
    {
        foreach (var r in responses)
            _responses.Enqueue(r);
    }

    public void EnqueueResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses.Enqueue((content, statusCode));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content is not null)
            RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        else
            RequestBodies.Add(null);

        var (content, statusCode) = _responses.Count > 0
            ? _responses.Dequeue()
            : ("{}", HttpStatusCode.OK);

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

public class CommsMockHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler);
}
