// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    internal static class HttpContextExtensions
    {
        private const int StreamCopyBufferSize = 81920;

        internal static async Task ProxyRequest(
            this HttpContext context,
            ProxyContext proxyContext,
            IHttpClientFactory httpClientFactory)
        {
            using (var requestMessage = proxyContext.ProxyRequest)
            {
                var httpClient = httpClientFactory.CreateClient("ProxyKit");

                try
                {
                    using (var responseMessage = await httpClient.SendAsync(
                        requestMessage,
                        HttpCompletionOption.ResponseHeadersRead,
                        context.RequestAborted))
                    {
                        await context.CopyProxyHttpResponse(responseMessage);
                    }
                }
                catch(TaskCanceledException ex)
                {
                    // Task cancelled exceptions can happen when either client disconnects before server has time to respond 
                    // or when the proxy request times out. 
                    if (RequestHasTimedOut(ex))
                    {
                        context.Response.StatusCode = 504; // GatewayTimeout
                        return;
                    }

                    throw;
                }
                catch (OperationCanceledException)
                {
                    // On Windows (kestrel), this can happen if the proxy timeout is set smaller than the 
                    // request takes to execute. On linux (docker) the code UpstreamIsUnavailable(ex) returns
                    // true. To make the behavior the same, we return a 503 (ServiceNotAvailable)
                    context.Response.StatusCode = 503; // ServiceNotAvailable
                }
                catch (HttpRequestException ex)
                {
                    if (UpstreamIsUnavailable(ex))
                    {
                        context.Response.StatusCode = 503; // ServiceNotAvailable
                        return;
                    }

                    throw;
                }
            }
        }
        
        private static bool UpstreamIsUnavailable(HttpRequestException ex)
        {
            return ex.InnerException is IOException || ex.InnerException is SocketException;
        }

        private static bool RequestHasTimedOut(OperationCanceledException ex)
        {
            return ex.InnerException is IOException;
        }

        internal static HttpRequestMessage CreateProxyHttpRequest(this HttpRequest request)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            /*requestMessage.Headers.Host = destinationUri.Authority;
            requestMessage.RequestUri = destinationUri;*/
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        private static async Task CopyProxyHttpResponse(this HttpContext context, HttpResponseMessage responseMessage)
        {
            var response = context.Response;

            response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
            response.Headers.Remove("transfer-encoding");

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                await responseStream.CopyToAsync(response.Body, StreamCopyBufferSize, context.RequestAborted);
            }
        }
    }
}
