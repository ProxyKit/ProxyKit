using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class ProxyContextExtensions
    {
        public static ForwardContext ForwardTo(this HttpContext conext, UpstreamHost upstreamHost)
        {
            var uri = new Uri(UriHelper.BuildAbsolute(
                upstreamHost.Scheme,
                upstreamHost.Host,
                upstreamHost.PathBase,
                conext.Request.Path,
                conext.Request.QueryString));

            var request = conext.Request.CreateProxyHttpRequest();
            request.Headers.Host = uri.Authority;
            request.RequestUri = uri;

            var proxyKitClient = conext
                .RequestServices
                .GetRequiredService<ProxyKitClient>();

            return new ForwardContext(conext, request, proxyKitClient.Client);
        }

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