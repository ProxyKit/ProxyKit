using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ForwardContext
    {
        public ForwardContext(
            HttpContext httpContext,
            HttpRequestMessage request)
        {
            HttpContext = httpContext;
            Request = request;
        }

        public HttpContext HttpContext { get; }

        public HttpRequestMessage Request { get; }
    }
}