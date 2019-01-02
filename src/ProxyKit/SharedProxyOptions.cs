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
        /// the handler can be re-created by HttpClientFactory, so has to be a function. 
        /// </summary>
        public Func<HttpMessageHandler> GetMessageHandler { get; set; }

        /// <summary>
        /// Configures the HttpClient.
        /// </summary>
        public Action<IServiceProvider, HttpClient> ConfigureHttpClient { get; set; }
    }
}
