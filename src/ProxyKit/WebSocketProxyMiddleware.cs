using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ProxyKit
{
    public class WebSocketProxyMiddleware
    {
        private static readonly HashSet<string> NotForwardedWebSocketHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Connection, HeaderNames.Host, HeaderNames.Upgrade, HeaderNames.SecWebSocketAccept,
            HeaderNames.SecWebSocketProtocol, HeaderNames.SecWebSocketKey, HeaderNames.SecWebSocketVersion,
            "Sec-WebSocket-Extensions"
        };

        private const int DefaultWebSocketBufferSize = 4096;
        private readonly RequestDelegate _next;
        private readonly ProxyOptions _options;
        private readonly ILogger<WebSocketProxyMiddleware> _logger;
        private readonly Func<HttpContext, UpstreamHost> _getUpstreamHost;
        private readonly Action<WebSocketClientOptions> _customizeWebSocketClient = _ => { };

        private WebSocketProxyMiddleware(
            RequestDelegate next,
            IOptionsMonitor<ProxyOptions> options,
            ILogger<WebSocketProxyMiddleware> logger)
        {
            _next = next;
            _options = options.CurrentValue;
            _logger = logger;
        }

        public WebSocketProxyMiddleware(
            RequestDelegate next,
            IOptionsMonitor<ProxyOptions> options,
            Uri upstreamUri,
            ILogger<WebSocketProxyMiddleware> logger) : this(next, options, logger)
        {
            _getUpstreamHost = _ => upstreamUri;
        }

        public WebSocketProxyMiddleware(
               RequestDelegate next,
               IOptionsMonitor<ProxyOptions> options,
               Func<HttpContext, UpstreamHost> getUpstreamHost,
               ILogger<WebSocketProxyMiddleware> logger) : this(next, options, logger)
        {
            _getUpstreamHost = getUpstreamHost;
        }

        public WebSocketProxyMiddleware(
            RequestDelegate next,
            IOptionsMonitor<ProxyOptions> options,
            Func<HttpContext, UpstreamHost> getUpstreamHost,
            Action<WebSocketClientOptions> customizeWebSocketClient,
            ILogger<WebSocketProxyMiddleware> logger) : this(next, options, logger)
        {
            _getUpstreamHost = getUpstreamHost;
            _customizeWebSocketClient = customizeWebSocketClient;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await ProxyOutToWebSocket(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        private Task ProxyOutToWebSocket(HttpContext context)
        {
            var relativePath = context.Request.Path.ToString().TrimStart('/');
            var upstreamUri = _getUpstreamHost(context);
            var uriWithPath = new Uri(
                upstreamUri.Uri,
                relativePath.Length >= 0 ? relativePath : "");

            var uriBuilder = new UriBuilder(uriWithPath)
            {
                Query = context.Request.QueryString.ToUriComponent()
            };

            _logger.LogInformation("Forwarding websocket connection to {0}", uriBuilder.Uri);

            return AcceptProxyWebSocketRequest(context, uriBuilder.Uri);
        }

        private async Task AcceptProxyWebSocketRequest(HttpContext context, Uri upstreamUri)
        {
            using (var client = new ClientWebSocket())
            {
                foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
                {
                    client.Options.AddSubProtocol(protocol);
                }

                foreach (var headerEntry in context.Request.Headers)
                {
                    if (!NotForwardedWebSocketHeaders.Contains(headerEntry.Key))
                    {
                        client.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                    }
                }

                if (_options.WebSocketKeepAliveInterval.HasValue)
                {
                    client.Options.KeepAliveInterval = _options.WebSocketKeepAliveInterval.Value;
                }

                _customizeWebSocketClient(new WebSocketClientOptions(client.Options, context));

                try
                {
                    await client.ConnectAsync(upstreamUri, context.RequestAborted).ConfigureAwait(false);
                }
                catch (WebSocketException ex)
                {
                    context.Response.StatusCode = 400;
                    _logger.LogError(ex, "Error connecting to server");
                    return;
                }

                using (var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol).ConfigureAwait(false))
                {
                    var bufferSize = _options.WebSocketBufferSize ?? DefaultWebSocketBufferSize;
                    await Task.WhenAll(
                        PumpWebSocket(client, server, bufferSize, context.RequestAborted),
                        PumpWebSocket(server, client, bufferSize, context.RequestAborted)).ConfigureAwait(false);
                }
            }
        }

        private static async Task PumpWebSocket(WebSocket source, WebSocket destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var buffer = new byte[bufferSize];
            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await source
                        .ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await destination.CloseOutputAsync(
                        WebSocketCloseStatus.EndpointUnavailable,
                        "Endpoint unavailable",
                        cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination
                        .CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }
                await destination.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}