using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ReserveInventory.NUnitTest.TestHelpers
{
    internal sealed class TestHttpResponseData : HttpResponseData
    {
        private readonly MemoryStream _body = new();

        public TestHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode)
            : base(functionContext)
        {
            StatusCode = statusCode;
            Headers = new HttpHeadersCollection();
            Body = _body;
        }

        public override HttpStatusCode StatusCode { get; set; }

        // HttpResponseData requires set accessors for Headers and Body in this Worker version
        public override HttpHeadersCollection Headers { get; set; }

        public override Stream Body { get; set; }

        public override HttpCookies Cookies => null!;

        public string GetBodyAsString()
        {
            Body.Position = 0;
            using var reader = new StreamReader(Body, Encoding.UTF8, leaveOpen: true);
            var text = reader.ReadToEnd();
            Body.Position = 0;
            return text;
        }

        // Provide an instance generic WriteAsJsonAsync so the runtime will call this method
        // instead of the extension method that relies on DI.
        public new ValueTask WriteAsJsonAsync<TValue>(TValue value, CancellationToken cancellationToken = default)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(json);
            Body.Write(bytes, 0, bytes.Length);
            Body.Position = 0;
            return new ValueTask();
        }
    }

    internal sealed class TestHttpRequestData : HttpRequestData
    {
        private readonly MemoryStream _body;

        public TestHttpRequestData(FunctionContext functionContext, string content)
            : base(functionContext)
        {
            _body = new MemoryStream();
            if (!string.IsNullOrEmpty(content))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                _body.Write(bytes, 0, bytes.Length);
                _body.Position = 0;
            }

            Body = _body;
            Headers = new HttpHeadersCollection();
        }

        public override Stream Body { get; }

        public override HttpHeadersCollection Headers { get; }

        public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();

        public override Uri Url => new Uri("http://localhost/");

        public override string Method => "POST";

        // The Worker SDK expects an Identities property on HttpRequestData; it uses ClaimsIdentity in this version
        public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();

        // Implement CreateResponse overloads to match Worker API and avoid extension-method fallbacks
        public override HttpResponseData CreateResponse()
            => new TestHttpResponseData(FunctionContext, HttpStatusCode.OK);

        // Some Functions call CreateResponse(HttpStatusCode); implement that overload to return our concrete response
        public HttpResponseData CreateResponse(HttpStatusCode statusCode)
            => new TestHttpResponseData(FunctionContext, statusCode);
    }
}
