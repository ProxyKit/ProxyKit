using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class ProxyContextExtensions
    {
        /// <summary>
        /// Forward the request to the specified upstream host.
        /// </summary>
        /// <param name="context">The HttpContext</param>
        /// <param name="upstreamHost">The upstream host to forward the requests to.</param>
        /// <returns>A <see cref="ForwardContext"/> that represents the forwarding request context.</returns>
        public static ForwardContext ForwardTo(this HttpContext context, UpstreamHost upstreamHost)
        {
            var uri = new Uri(UriHelper.BuildAbsolute(
                upstreamHost.Scheme,
                upstreamHost.Host,
                upstreamHost.PathBase,
                context.Request.Path,
                context.Request.QueryString));

            var request = context.Request.CreateProxyHttpRequest();
            request.Headers.Host = uri.Authority;
            request.RequestUri = uri;

            var proxyKitClient = context
                .RequestServices
                .GetRequiredService<ProxyKitClient>();

            return new ForwardContext(context, request, proxyKitClient.Client);
        }
        
        /// <summary>
        /// Applies X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and X-Forwarded-PathBase headers
        /// to the forward request context.
        /// </summary>
        /// <param name="forwardContext">The forward context.</param>
        /// <returns>The forward context.</returns>
        public static ForwardContext ApplyXForwardedHeaders(this ForwardContext forwardContext)
        {
            var headers = forwardContext.UpstreamRequest.Headers;
            var protocol = forwardContext.HttpContext.Request.Scheme;
            var @for = forwardContext.HttpContext.Connection.RemoteIpAddress;
            var host = forwardContext.HttpContext.Request.Headers["Host"];
            var hostString = HostString.FromUriComponent(host);
            var pathBase = forwardContext.HttpContext.Request.PathBase.Value;

            headers.ApplyXForwardedHeaders(@for, hostString, protocol, pathBase);

            return forwardContext;
        }
    }
}