using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public delegate Task<Uri> HandleWebSocketProxyRequest(HttpContext httpContext);
}