using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace ProxyKit
{
    public static class ProxyContextExtensions
    {
        /// <summary>
        ///     Applies X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host and X-Forwarded-PathBase
        ///     headers.
        /// </summary>
        /// <param name="context"></param>
        public static void ApplyXForwardedHeaders(this ProxyContext context)
        {
            var headers = context.ProxyRequest.Headers;
            var protocol = context.IncomingRequest.Scheme;
            var @for = context.Connection.RemoteIpAddress;
            var host = context.IncomingRequest.Headers["Host"];
            var hostString = HostString.FromUriComponent(host);
            var pathBase = context.IncomingRequest.PathBase.Value; // TODO should be escaped?

            headers.ApplyXForwardedHeaders(@for, hostString, protocol, pathBase);
        }

        public static void ForwardTo(
            this ProxyContext context,
            Uri destinationUri)
        {
            var uri = new Uri(UriHelper.BuildAbsolute(
                destinationUri.Scheme,
                new HostString(destinationUri.Host, destinationUri.Port),
                destinationUri.AbsolutePath,
                context.IncomingRequest.Path,
                context.IncomingRequest.QueryString));

            context.ProxyRequest.Headers.Host = uri.Authority;
            context.ProxyRequest.RequestUri = uri;
        }

        public static void ForwardTo(
            this ProxyContext context,
            string scheme,
            HostString host,
            PathString pathBase = default)
        {
            var uri = new Uri(UriHelper.BuildAbsolute(
                scheme,
                host,
                pathBase,
                context.IncomingRequest.Path,
                context.IncomingRequest.QueryString));

            context.ProxyRequest.Headers.Host = uri.Authority;
            context.ProxyRequest.RequestUri = uri;
        }

        public static void ForwardTo(this ProxyContext context, string destinationUri)
        {
            context.ForwardTo(new Uri(destinationUri));
        }
    }
}