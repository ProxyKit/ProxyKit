// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ConventionalProxyMiddleware<TProxyHandler> : ProxyMiddleware
        where TProxyHandler:IProxyHandler
    {
        private readonly TProxyHandler _handler;

        public ConventionalProxyMiddleware(RequestDelegate _, TProxyHandler handler)
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
    }
}
