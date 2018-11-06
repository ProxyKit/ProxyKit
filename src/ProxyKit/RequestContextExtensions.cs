using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace ProxyKit
{
    public static class RequestContextExtensions
    {
        /// <summary>
        ///     Generates a URI to forward the request to. The incoming path and
        ///     querystring are appended.
        /// </summary>
        /// <param name="requestContext">The incoming HTTP request context.</param>
        /// <param name="scheme">The destination scheme.</param>
        /// <param name="host">The destination host.</param>
        /// <param name="pathBase">The destination path base.</param>
        /// <returns></returns>
        public static Uri ForwardTo(
            this RequestContext requestContext,
            string scheme,
            HostString host,
            PathString pathBase = default) 
            => new Uri(UriHelper.BuildAbsolute(
                scheme,
                host,
                pathBase,
                requestContext.Path,
                requestContext.QueryString));

        /// <summary>
        ///     Generates a URI to forward the request to. The incoming path and
        ///     querystring are appended.
        /// </summary>
        /// <param name="requestContext">The incoming HTTP request context.</param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri ForwardTo(
            this RequestContext requestContext,
            Uri uri)
            => new Uri(UriHelper.BuildAbsolute(
                uri.Scheme,
                new HostString(uri.Host,uri.Port),
                uri.AbsolutePath,
                requestContext.Path,
                requestContext.QueryString));

        /// <summary>
        ///     Generates a URI to forward the request to. The incoming path and
        ///     querystring are appended.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="uriString"></param>
        /// <returns></returns>
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