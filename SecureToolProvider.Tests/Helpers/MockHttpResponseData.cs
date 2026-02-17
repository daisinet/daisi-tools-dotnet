using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SecureToolProvider.Tests.Helpers;

/// <summary>
/// Minimal mock HttpResponseData for testing Azure Functions HTTP triggers.
/// </summary>
public class MockHttpResponseData : HttpResponseData
{
    public MockHttpResponseData(FunctionContext context)
        : base(context)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies => throw new NotImplementedException();
}
