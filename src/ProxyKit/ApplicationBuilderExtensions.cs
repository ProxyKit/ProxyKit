// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        /// <param name="prepareRequest">
        ///     A delegate to allow modification of the request prior to
        ///     forwarding.
        /// </param>
        public static void RunProxy(
            this IApplicationBuilder app,
            PathString pathMatch,
            HandleProxyRequest handleProxyRequest,
            PrepareRequest prepareRequest = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var proxyOptions = new ProxyOptions
            {
                HandleProxyRequest = handleProxyRequest,
                PrepareRequest = prepareRequest
            };

            app.Map(pathMatch, appInner =>
            {
                appInner.UseMiddleware<ProxyMiddleware>(proxyOptions);
            });
        }

        /// <summary>
        /// Runs proxy forwarding requests to downstream server.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="handleProxyRequest">
        ///     A delegate that can resolve the destination Uri.
        /// </param>
        /// <param name="prepareRequest">
        ///     A delegate to allow modification of the request prior to
        ///     forwarding.
        /// </param>
        public static void RunProxy(
            this IApplicationBuilder app,
            HandleProxyRequest handleProxyRequest,
            PrepareRequest prepareRequest = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var proxyOptions = new ProxyOptions
            {
                HandleProxyRequest = handleProxyRequest,
                PrepareRequest = prepareRequest
            };

            app.UseMiddleware<ProxyMiddleware>(proxyOptions);
        }

        public static void RunProxy2(
            this IApplicationBuilder app,
            HandleProxyRequest2 handleProxyRequest)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var proxyOptions = new ProxyOptions2
            {
                HandleProxyRequest = handleProxyRequest,
            };

            app.UseMiddleware<ProxyMiddleware>(proxyOptions);
        }
    }

    public class ProxyContext
    {
        public ConnectionInfo ConnectionInfo { get; }

        public IncomingRequest IncomingRequest { get; }

        public HttpRequestMessage OutgoingRequestMessage { get; }
    }

    public delegate Task HandleProxyRequest2(ProxyContext proxyContext, ForwardToHost forwardToHost);

    public delegate Task ForwardToHost(ProxyContext proxyContext, string targetUri);
}
