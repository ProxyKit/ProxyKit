// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    /// <summary>
    /// Proxy Middleware
    /// </summary>
    public class ProxyMiddleware
    {
        private readonly ProxyOptions _proxyOptions;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProxyMiddleware(RequestDelegate _,
            ProxyOptions proxyOptions,
            IHttpClientFactory httpClientFactory)
        {
            _proxyOptions = proxyOptions;
            _httpClientFactory = httpClientFactory;
        }

        public Task Invoke(HttpContext context)
        {
            var requestContext = new RequestContext(context.Request);
            var proxyReponse = _proxyOptions.HandleProxyRequest(requestContext);

            if (proxyReponse.StatusCode != null)
            {
                context.Response.StatusCode = (int)proxyReponse.StatusCode;
                return Task.CompletedTask;
            }

            return context.ProxyRequest(proxyReponse.DestinationUri, _proxyOptions, _httpClientFactory);
        }
    }
}
