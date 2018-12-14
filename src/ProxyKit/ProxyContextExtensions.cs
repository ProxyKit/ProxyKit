using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace ProxyKit
{
    public static class ProxyContextExtensions
    {
        public static ForwardContext ForwardTo(this ProxyContext proxyContext, string destinationUri)
        {
            var destUri = new Uri(destinationUri);
            var uri = new Uri(UriHelper.BuildAbsolute(
                destUri.Scheme,
                new HostString(destUri.Host, destUri.Port),
                destUri.AbsolutePath,
                proxyContext.IncomingRequest.Path,
                proxyContext.IncomingRequest.QueryString));

            var request = proxyContext.IncomingRequest.CreateProxyHttpRequest();
            request.Headers.Host = uri.Authority;
            request.RequestUri = uri;

            return new ForwardContext(proxyContext, request);
        }

        public static ForwardContext ApplyXForwardedHeaders(this ForwardContext forwardContext)
        {
            var headers = forwardContext.Request.Headers;
            var protocol = forwardContext.ProxyContext.IncomingRequest.Scheme;
            var @for = forwardContext.ProxyContext.Connection.RemoteIpAddress;
            var host = forwardContext.ProxyContext.IncomingRequest.Headers["Host"];
            var hostString = HostString.FromUriComponent(host);
            var pathBase = forwardContext.ProxyContext.IncomingRequest.PathBase.Value;

            headers.ApplyXForwardedHeaders(@for, hostString, protocol, pathBase);

            return forwardContext;
        }
    }
}