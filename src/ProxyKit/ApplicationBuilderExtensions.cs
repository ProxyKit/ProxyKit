using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Runs proxy forwarding requests to downstream server.
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

            app.UseMiddleware<ProxyMiddleware>(handleProxyRequest);
        }

        /// <summary>
        /// Runs proxy forwarding requests to downstream server.
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

            app.Map(pathMatch, appInner =>
            {
                appInner.UseMiddleware<ProxyMiddleware>(handleProxyRequest);
            });
        }

        public static void UseWebSocketProxy(
            this IApplicationBuilder app,
            Uri destinationUri)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (destinationUri == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<WebSocketProxyMiddleware>(destinationUri);
        }
    }
}
