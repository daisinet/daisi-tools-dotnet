using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SecureToolProvider.Tests.Helpers;

/// <summary>
/// Minimal mock HttpRequestData for testing Azure Functions HTTP triggers.
/// </summary>
public class MockHttpRequestData : HttpRequestData
{
    private readonly FunctionContext _context;
    private readonly Stream _body;
    private readonly Uri _url;
    private readonly HttpHeadersCollection _headers;

    public MockHttpRequestData(FunctionContext context, Uri url, Stream? body = null)
        : base(context)
    {
        _context = context;
        _url = url;
        _body = body ?? new MemoryStream();
        _headers = new HttpHeadersCollection();
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers => _headers;
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url => _url;
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method => _url.Query.Length > 0 ? "GET" : "POST";

    public override HttpResponseData CreateResponse()
    {
        return new MockHttpResponseData(_context);
    }
}
