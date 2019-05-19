using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProxyKit
{
    public class WebSocketProxyMiddleware
    {
        private static readonly HashSet<string> NotForwardedWebSocketHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connection", "Host", "Upgrade", "Sec-WebSocket-Accept",
            "Sec-WebSocket-Protocol", "Sec-WebSocket-Key", "Sec-WebSocket-Version",
            "Sec-WebSocket-Extensions"
        };
        private const int DefaultWebSocketBufferSize = 4096;
        private readonly RequestDelegate _next;
        private readonly ProxyOptions _options;
        private readonly ILogger<WebSocketProxyMiddleware> _logger;
        private readonly Func<HttpContext, Uri> _getUpstreamUri;

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
            _getUpstreamUri = _ => upstreamUri;
        }

        public WebSocketProxyMiddleware(
               RequestDelegate next,
               IOptionsMonitor<ProxyOptions> options,
               Func<HttpContext, Uri> getUpstreamUri,
               ILogger<WebSocketProxyMiddleware> logger) : this(next, options, logger)
        {
            _getUpstreamUri = getUpstreamUri;
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
            var relativePath = context.Request.Path.ToString();
            var upstreamUri = _getUpstreamUri(context);
            var uri = new Uri(
                upstreamUri,  
                relativePath.Length >= 0 ? relativePath : "");

            _logger.LogInformation("Forwarding websocket connection to {0}", uri);

            return AcceptProxyWebSocketRequest(context, uri);
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
                    result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken)
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