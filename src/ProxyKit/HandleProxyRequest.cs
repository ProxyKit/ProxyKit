using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProxyKit
{
    public delegate Task<HttpResponseMessage> HandleProxyRequest(
        ProxyContext proxyContext,
        Func<ForwardContext, Task<HttpResponseMessage>> handle);
}