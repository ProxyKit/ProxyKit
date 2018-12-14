using System.Net.Http;

namespace ProxyKit
{
    public class ForwardContext
    {
        public ForwardContext(
            ProxyContext proxyContext,
            HttpRequestMessage request)
        {
            ProxyContext = proxyContext;
            Request = request;
        }

        public ProxyContext ProxyContext { get; }

        public HttpRequestMessage Request { get; }
    }
}