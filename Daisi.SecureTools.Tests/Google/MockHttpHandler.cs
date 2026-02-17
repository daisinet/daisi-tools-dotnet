using System.Net;

namespace Daisi.SecureTools.Tests.Google;

/// <summary>
/// Mock HTTP message handler for testing Google API calls.
/// Can be configured with multiple responses for sequential requests.
/// </summary>
public class MockHttpHandler : HttpMessageHandler
{
    private readonly Queue<(string Content, HttpStatusCode StatusCode)> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;
    public HttpRequestMessage? LastRequest => _requests.Count > 0 ? _requests[^1] : null;

    public MockHttpHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses.Enqueue((responseContent, statusCode));
    }

    public MockHttpHandler(IEnumerable<(string Content, HttpStatusCode StatusCode)> responses)
    {
        foreach (var r in responses)
            _responses.Enqueue(r);
    }

    public void AddResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses.Enqueue((content, statusCode));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        var (content, statusCode) = _responses.Count > 0
            ? _responses.Dequeue()
            : ("{}", HttpStatusCode.OK);

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
