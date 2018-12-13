using System;
using System.Threading.Tasks;

namespace ProxyKit
{
    public delegate Task HandleProxyRequestDelegate(ProxyContext proxyContext, Func<Task> handle);
}