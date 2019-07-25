// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class FactoryActivatedProxyMiddleware<TProxyHandler> : ProxyMiddleware, IMiddleware
        where TProxyHandler : IProxyHandler
    {
        private readonly TProxyHandler _handler;

        public FactoryActivatedProxyMiddleware(TProxyHandler handler)
        {
            _handler = handler;
        }
        
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using (var response = await _handler.HandleProxyRequest(context).ConfigureAwait(false))
            {
                await CopyProxyHttpResponse(context, response).ConfigureAwait(false);
            }
        }
    }
}
