using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public static class WebSocketClientOptionsExtensions
    {
        /// <summary>
        ///     Adds X-Forwarded-* headers to the upstream websocket request
        ///     with an additional PathBase parameter.
        /// </summary>
        public static void AddXForwardedHeaders(this WebSocketClientOptions options)
        {
            var protocol = options.HttpContext.Request.Scheme;
            var @for = options.HttpContext.Connection.RemoteIpAddress;
            var host = options.HttpContext.Request.Headers["Host"];
            var hostString = HostString.FromUriComponent(host);
            var pathBase = options.HttpContext.Request.PathBase.Value;

            options.AddXForwardedHeaders(@for, hostString, protocol, pathBase);
        }
    }
}