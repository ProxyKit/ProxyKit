using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class WebSocketForwardContext
    {
        private readonly HttpContext _context;
        private readonly Uri _upstreamUri;

        internal WebSocketForwardContext(
            HttpContext context,
            ClientWebSocket clientWebSocket,
            Uri upstreamUri)
        {
            _context = context;
            _upstreamUri = upstreamUri;
        }
    }
}