using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        ///     Runs proxy forwarding requests to downstream server.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="handleProxyRequest">
        ///     A delegate that can resolve the destination Uri.
        /// </param>
        public static void RunProxy(
            this IApplicationBuilder app,
            HandleProxyRequest handleProxyRequest)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (handleProxyRequest == null)
            {
                throw new ArgumentNullException(nameof(handleProxyRequest));
            }

            app.UseMiddleware<ProxyMiddleware>(handleProxyRequest);
        }

        /// <summary>
        ///     Runs proxy forwarding requests to downstream server.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="pathMatch">
        ///     Branches the request pipeline based on matches of the given
        ///     request path. If the request path starts with the given path,
        ///     the branch is executed.
        /// </param>
        /// <param name="handleProxyRequest">
        ///     A delegate that can resolve the destination Uri.
        /// </param>
        public static void RunProxy(
            this IApplicationBuilder app,
            PathString pathMatch,
            HandleProxyRequest handleProxyRequest)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (handleProxyRequest == null)
            {
                throw new ArgumentNullException(nameof(handleProxyRequest));
            }

            app.Map(pathMatch, appInner => { appInner.UseMiddleware<ProxyMiddleware>(handleProxyRequest); });
        }


        /// <summary>
        ///     Adds WebSocket proxy that forwards websocket connections
        ///     to destination Uri.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="upstreamUri">
        ///     The uri to forward the websocket connection to. Must start with
        ///     ws:// or wss://
        /// </param>
        public static void UseWebSocketProxy(
            this IApplicationBuilder app,
            Uri upstreamUri)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (upstreamUri == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<WebSocketProxyMiddleware>(upstreamUri);
        }

        /// <summary>
        ///     Adds WebSocket proxy that forwards websocket connections
        ///     to destination Uri based the HttpContext if the request matches
        ///     a given path.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="urlPath">
        ///     The url path the request must match for the websocket request to be forwarded.
        /// </param>
        /// <param name="getUpstreamUri">
        ///     The Uri selection function.
        /// </param>
        public static void UseWebSocketProxy(
            this IApplicationBuilder app,
            string urlPath,
            Func<HttpContext, Uri> getUpstreamUri)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrWhiteSpace(urlPath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(urlPath));
            }

            if (getUpstreamUri == null)
            {
                throw new ArgumentNullException(nameof(getUpstreamUri));
            }

            app.UseMiddleware<WebSocketProxyMiddleware>(urlPath, getUpstreamUri);
        }
    }
}