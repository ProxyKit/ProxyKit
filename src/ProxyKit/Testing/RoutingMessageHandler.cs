using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyKit.Testing
{
    /// <summary>
    ///     An <see cref="HttpMessageHandler"/> that acts like a router
    ///     between multiple handlers that represent different hosts.
    /// </summary>
    public class RoutingMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HostHandler> _hosts
            = new Dictionary<string, HostHandler>();

        /// <summary>
        ///     Adds a handler for a given Uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="handler"></param>
        public void AddHandler(Uri uri, HttpMessageHandler handler)
        {
            var endpoint = new HostHandler(handler);
            var host = $"{uri.Host}:{uri.Port}";
            _hosts.Add(host, endpoint);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var host = $"{request.RequestUri.Host}:{request.RequestUri.Port}";
            var hostHandler = _hosts[host];

            return hostHandler.Send(request, cancellationToken);
        }

        private class HostHandler : DelegatingHandler
        {
            public HostHandler(HttpMessageHandler innerHandler) 
                : base(innerHandler)
            {}

            internal Task<HttpResponseMessage> Send(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken);
            }
        }
    }
}
