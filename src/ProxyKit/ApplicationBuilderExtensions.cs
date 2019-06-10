using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        ///     Runs a reverse proxy forwarding requests to an upstream host.
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

            app.UseMiddleware<ProxyMiddleware<HandleProxyRequestWrapper>>(new HandleProxyRequestWrapper(handleProxyRequest));
        }


        /// <summary>
        ///     Runs a reverse proxy forwarding requests to an upstream host.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        public static void RunProxy<TProxyHandler>(this IApplicationBuilder app) 
            where TProxyHandler : IProxyHandler
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<ProxyMiddleware<TProxyHandler>>();
        }

        /// <summary>
        ///     Runs reverse proxy forwarding requests to an upstream host.
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
        [Obsolete("Use app.Map(\"/path\", appProxy=> { appProxy.UseProxy(...); } instead. " +
                  "This will be removed in a future version", false)]
        public static void RunProxy(
            this IApplicationBuilder app,
            PathString pathMatch,
            HandleProxyRequest handleProxyRequest)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.Map(pathMatch, appInner =>
            {
                appInner.UseMiddleware<ProxyMiddleware<HandleProxyRequestWrapper>>(new HandleProxyRequestWrapper(handleProxyRequest));
            });
        }

        /// <summary>
        ///     Adds WebSocket proxy that forwards websocket connections
        ///     to destination Uri based the HttpContext if the request matches
        ///     a given path.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="getUpstreamUri">
        ///     A function to get the uri to forward the websocket connection to. The
        ///     result of which must start with ws:// or wss://
        /// </param>
        public static void UseWebSocketProxy(
            this IApplicationBuilder app, 
            Func<HttpContext, UpstreamHost> getUpstreamUri)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (getUpstreamUri == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<WebSocketProxyMiddleware>(getUpstreamUri);
        }

        /// <summary>
        ///     Adds WebSocket proxy that forwards websocket connections
        ///     to destination Uri based the HttpContext if the request matches
        ///     a given path.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="getUpstreamUri">
        ///     A function to get the uri to forward the websocket connection to. The
        ///     result of which must start with ws:// or wss://
        /// </param>
        /// <param name="configureClientOptions">
        ///     An action to allow customizing of the websocket client before initial
        ///     connection allowing you to set custom headers or adjust cookies.
        /// </param>
        public static void UseWebSocketProxy(
            this IApplicationBuilder app,
            Func<HttpContext, UpstreamHost> getUpstreamUri,
            Action<WebSocketClientOptions> configureClientOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (getUpstreamUri == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<WebSocketProxyMiddleware>(getUpstreamUri, configureClientOptions);
        }

        private class HandleProxyRequestWrapper : IProxyHandler
        {
            private readonly HandleProxyRequest _handleProxyRequest;

            public HandleProxyRequestWrapper(HandleProxyRequest handleProxyRequest)
            {
                _handleProxyRequest = handleProxyRequest;
            }

            public Task<HttpResponseMessage> HandleProxyRequest(HttpContext httpContext)
                => _handleProxyRequest(httpContext);
        }
    }
}