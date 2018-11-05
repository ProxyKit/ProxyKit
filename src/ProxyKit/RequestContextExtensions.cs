using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace ProxyKit
{
    public static class RequestContextExtensions
    {
        /// <summary>
        ///     Generates a URI to forward the request to. The incoming path and
        ///     querystring are appened
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="scheme"></param>
        /// <param name="host"></param>
        /// <param name="pathBase"></param>
        /// <returns></returns>
        public static Uri ForwardTo(
            this RequestContext requestContext,
            string scheme,
            HostString host,
            PathString pathBase = default(PathString)) 
            => new Uri(UriHelper.BuildAbsolute(
                scheme,
                host,
                pathBase,
                requestContext.Path,
                requestContext.QueryString));

        public static Uri ForwardTo(
            this RequestContext requestContext,
            Uri uri)
            => new Uri(UriHelper.BuildAbsolute(
                uri.Scheme,
                new HostString(uri.Host,uri.Port),
                uri.AbsolutePath,
                requestContext.Path,
                requestContext.QueryString));

        public static Uri ForwardTo(
            this RequestContext requestContext,
            string uriString)
        {
            var uri = new Uri(uriString);
            return new Uri(UriHelper.BuildAbsolute(
                uri.Scheme,
                new HostString(uri.Host, uri.Port),
                uri.AbsolutePath,
                requestContext.Path,
                requestContext.QueryString));
        }
    }
}