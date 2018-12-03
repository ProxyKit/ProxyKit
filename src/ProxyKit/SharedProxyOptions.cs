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
        /// Builds a message handler used for http message forwarding.
        /// the handler can be re-created by aspnet core, so has to be a function. 
        /// </summary>
        public Func<HttpMessageHandler> GetMessageHandler { get; set; }

        public Action<IServiceProvider, HttpClient> ConfigureHttpClient { get; set; }
}
}
