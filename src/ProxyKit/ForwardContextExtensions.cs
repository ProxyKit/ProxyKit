using System;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace ProxyKit
{
    public static class ForwardContextExtensions
    {
        [Obsolete("Use AddXForwardedHeaders() instead.", true)]
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
            var host = forwardContext.HttpContext.Request.Headers[HeaderNames.Host];
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