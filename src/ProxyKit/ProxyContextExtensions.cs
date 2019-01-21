using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class ProxyContextExtensions
    {
        /// <summary>
        ///     Forward the request to the specified upstream host.
        /// </summary>
        /// <param name="context">The HttpContext</param>
        /// <param name="upstreamHost">The upstream host to forward the requests
        /// to.</param>
        /// <returns>A <see cref="ForwardContext"/> that represents the
        /// forwarding request context.</returns>
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
        ///     Applies X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and
        ///     X-Forwarded-PathBase headers to the forward request context. If
        ///     the headers already exist, they will be appended, otherwise they
        ///     will be added.
        /// </summary>
        /// <param name="forwardContext">The forward context.</param>
        /// <returns>The forward context.</returns>
        [Obsolete("Use AddXForwardedHeaders() instead. This will be removed in a future version", false)]
        public static ForwardContext ApplyXForwardedHeaders(this ForwardContext forwardContext) =>
            forwardContext.AddXForwardedHeaders();

        /// <summary>
        ///     Adds X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and
        ///     X-Forwarded-PathBase headers to the forward request context. If
        ///     the headers already exist they will be appended otherwise they
        ///     will be added.
        /// </summary>
        /// <param name="forwardContext">The forward context.</param>
        /// <returns>The forward context.</returns>
        public static ForwardContext AddXForwardedHeaders(this ForwardContext forwardContext)
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

        /// <summary>
        ///     Copies X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and
        ///     X-Forwarded-PathBase headers to the forward request context from
        ///     the incoming request. This should only be performed if this. If
        ///     the headers already exist, they will be appended.
        /// </summary>
        /// <param name="forwardContext">The forward context.</param>
        /// <returns>The forward context.</returns>
        public static ForwardContext CopyXForwardedHeaders(this ForwardContext forwardContext)
        {
            var headers = forwardContext.UpstreamRequest.Headers;

            if (forwardContext.HttpContext.Request.Headers.TryGetValue(XForwardedExtensions.XForwardedFor, out var forValues))
            {
                headers.Remove(XForwardedExtensions.XForwardedFor);
                headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedFor, forValues.ToArray());
            }

            if (forwardContext.HttpContext.Request.Headers.TryGetValue(XForwardedExtensions.XForwardedHost, out var hostValues))
            {
                headers.Remove(XForwardedExtensions.XForwardedHost);
                headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedHost, hostValues.ToArray());
            }

            if (forwardContext.HttpContext.Request.Headers.TryGetValue(XForwardedExtensions.XForwardedProto, out var protoValues))
            {
                headers.Remove(XForwardedExtensions.XForwardedProto);
                headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedProto, protoValues.ToArray());
            }

            if (forwardContext.HttpContext.Request.Headers.TryGetValue(XForwardedExtensions.XForwardedPathBase, out var pathBaseValues))
            {
                headers.Remove(XForwardedExtensions.XForwardedPathBase);
                headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedPathBase, pathBaseValues.ToArray());
            }

            return forwardContext;
        }
    }
}