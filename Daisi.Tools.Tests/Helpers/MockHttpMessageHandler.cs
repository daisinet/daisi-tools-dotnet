using System.Net;

namespace Daisi.Tools.Tests.Helpers
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }

    public class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    public class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public MockHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler);
        }
    }
}
