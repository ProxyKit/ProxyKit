// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
            var proxyContext = new ProxyContext(
                context.Connection,
                context.Request,
                context.RequestAborted,
                context.RequestServices);

            var httpClient = _httpClientFactory.CreateClient("ProxyKit");

            async Task<HttpResponseMessage> Handle(ForwardContext forwardContext)
            {
                try
                {
                    return await httpClient.SendAsync(
                        forwardContext.Request,
                        HttpCompletionOption.ResponseHeadersRead,
                        context.RequestAborted);
                }
                catch (TaskCanceledException ex)
                {
                    // Task cancelled exceptions can happen when either client disconnects before server has time to respond 
                    // or when the proxy request times out. 
                    if (RequestHasTimedOut(ex))
                    {
                        return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
                    }

                    throw;
                }
                catch (OperationCanceledException)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
                catch (HttpRequestException ex)
                {
                    if (UpstreamIsUnavailable(ex))
                    {
                        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                    }

                    throw;
                }
            }

            using (var response = await _proxyOptions.HandleProxyRequest(proxyContext, Handle))
            {
                await CopyProxyHttpResponse(context, response);
            }
        }

        private static bool RequestHasTimedOut(OperationCanceledException ex) 
            => ex.InnerException is IOException;

        private static bool UpstreamIsUnavailable(HttpRequestException ex) 
            => ex.InnerException is IOException || ex.InnerException is SocketException;

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
