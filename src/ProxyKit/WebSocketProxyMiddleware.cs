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
        private readonly HandleWebSocketProxyRequest _handleWebSocketProxyRequest;
        private readonly ProxyOptions _options;
        private readonly ILogger<WebSocketProxyMiddleware> _logger;

        public WebSocketProxyMiddleware(
            RequestDelegate next,
            IOptionsMonitor<ProxyOptions> options,
            HandleWebSocketProxyRequest handleWebSocketProxyRequest,
            ILogger<WebSocketProxyMiddleware> logger)
        {
            _next = next;
            _handleWebSocketProxyRequest = handleWebSocketProxyRequest;
            _options = options.CurrentValue;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await AcceptProxyWebSocketRequest(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        private async Task AcceptProxyWebSocketRequest(HttpContext context)
        {
            var upstreamUri = await _handleWebSocketProxyRequest(context).ConfigureAwait(false);

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
                    var uriBuilder = new UriBuilder(upstreamUri.Scheme, upstreamUri.Host, upstreamUri.Port, context.Request.Path, context.Request.QueryString.Value);
                    await client.ConnectAsync(uriBuilder.Uri, context.RequestAborted).ConfigureAwait(false);
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
                        "Endpoind unavailable",
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