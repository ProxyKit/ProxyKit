// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ProxyMiddleware<TProxyHandler> where TProxyHandler:IProxyHandler
    {
        private readonly TProxyHandler _handler;
        private const int StreamCopyBufferSize = 81920;

        public ProxyMiddleware(RequestDelegate _, TProxyHandler handler)
        {
            _handler = handler;
        }

        public async Task Invoke(HttpContext context)
        {
            using (var response = await _handler.HandleProxyRequest(context).ConfigureAwait(false))
            {
                await CopyProxyHttpResponse(context, response).ConfigureAwait(false);
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
                using (var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    await responseStream.CopyToAsync(response.Body, StreamCopyBufferSize, context.RequestAborted).ConfigureAwait(false);
                    await responseStream.FlushAsync(context.RequestAborted).ConfigureAwait(false);
                }
            }
        }
    }
}
