// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
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
        private const int StreamCopyBufferSize = 81920;

        public ProxyMiddleware(RequestDelegate _,
            ProxyOptions proxyOptions,
            IHttpClientFactory httpClientFactory)
        {
            _proxyOptions = proxyOptions;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            using (var response = await _proxyOptions.HandleProxyRequest(context))
            {
                await CopyProxyHttpResponse(context, response);
            }
        }

        private static async Task CopyProxyHttpResponse(HttpContext context, HttpResponseMessage responseMessage)
        {
            var response = context.Response;

            response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            if (responseMessage.Content != null)
            {
                foreach (var header in responseMessage.Content.Headers)
                {
                    response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
            response.Headers.Remove("transfer-encoding");

            if (responseMessage.Content != null)
            {
                using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
                {
                    await responseStream.CopyToAsync(response.Body, StreamCopyBufferSize, context.RequestAborted);
                }
            }
        }
    }
}
