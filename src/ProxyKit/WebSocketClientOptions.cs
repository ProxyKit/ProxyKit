using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class WebSocketClientOptions
    {
        private readonly ClientWebSocketOptions _options;

        internal WebSocketClientOptions(
            ClientWebSocketOptions options,
            HttpContext httpContext)
        {
            _options = options;
            HttpContext = httpContext;
        }

        /// <summary>
        /// Get or sets the cookies associated with the upstream websocket request.
        /// </summary>
        public CookieContainer Cookies
        {
            get => _options.Cookies;
            set => _options.Cookies = value;
        }

        /// <summary>
        ///     The incoming HttpContext.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        ///     Set a header on the upstream websocket request.
        /// </summary>
        /// <param name="headerName"></param>
        /// <param name="headerValue"></param>
        public void SetRequestHeader(string headerName, string headerValue)
        {
            _options.SetRequestHeader(headerName, headerValue);
        }
    }
}