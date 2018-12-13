using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ProxyContext
    {
        public ProxyContext(
            ConnectionInfo connection, 
            HttpRequest request,
            HttpRequestMessage proxyRequest,
            HttpResponse response)
        {
            Connection = connection;
            IncomingRequest = request;
            ProxyRequest = proxyRequest;
            Response = response;
        }

        public ConnectionInfo Connection { get; }

        public HttpRequest IncomingRequest { get; }

        public HttpRequestMessage ProxyRequest { get; }

        public HttpResponse Response { get; }
    }
}