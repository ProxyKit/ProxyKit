// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;

namespace ProxyKit
{
    /// <summary>
    /// Shared Proxy Options
    /// </summary>
    public class SharedProxyOptions
    {
        /// <summary>
        /// Message handler used for http message forwarding.
        /// </summary>
        public HttpMessageHandler MessageHandler { get; set; } =
            new HttpClientHandler {AllowAutoRedirect = false, UseCookies = false};

        public Action<IServiceProvider, HttpClient> ConfigureHttpClient { get; set; }
}
}
