using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyKit.Testing
{
    public class RoutingMessageHandler : HttpMessageHandler
    {
        delegate Task<HttpResponseMessage> HandlerSendAsync(
            HttpRequestMessage message,
            CancellationToken token);

        private readonly Dictionary<string, HandlerSendAsync> _hosts
            = new Dictionary<string, HandlerSendAsync>();

        public void AddHandler(Uri uri, HttpMessageHandler handler)
        {
            var nextDelegate = (HandlerSendAsync)
                handler.GetType()
                    .GetTypeInfo()
                    .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                    .CreateDelegate(typeof(HandlerSendAsync), handler);

            var host = $"{uri.Host}:{uri.Port}";
            _hosts.Add(host, nextDelegate);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var host = $"{request.RequestUri.Host}:{request.RequestUri.Port}";
            var nextDelegate = _hosts[host];

            return nextDelegate(request, cancellationToken);
        }
    }
}
