using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class ProxyContextExtensions
    {
        public static ForwardContext ForwardTo(this HttpContext conext, string destinationUri)
        {
            var destUri = new Uri(destinationUri);
            var uri = new Uri(UriHelper.BuildAbsolute(
                destUri.Scheme,
                new HostString(destUri.Host, destUri.Port),
                destUri.AbsolutePath,
                conext.Request.Path,
                conext.Request.QueryString));

            var request = conext.Request.CreateProxyHttpRequest();
            request.Headers.Host = uri.Authority;
            request.RequestUri = uri;

            var httpClientFactory = conext.RequestServices.GetRequiredService<IHttpClientFactory>();

            return new ForwardContext(conext, request, httpClientFactory);
        }

        public static ForwardContext ApplyXForwardedHeaders(this ForwardContext forwardContext)
        {
            var headers = forwardContext.Request.Headers;
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