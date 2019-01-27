using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ForwardContext
    {
        private readonly HttpClient _httpClient;

        internal ForwardContext(
            HttpContext httpContext,
            HttpRequestMessage upstreamRequest,
            HttpClient httpClient)
        {
            _httpClient = httpClient;
            HttpContext = httpContext;
            UpstreamRequest = upstreamRequest;
        }

        /// <summary>
        /// The incoming HttpContext
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The upstream request message.
        /// </summary>
        public HttpRequestMessage UpstreamRequest { get; }

        [Obsolete("Use Send() instead.", true)]
        public Task<HttpResponseMessage> Execute() => Send();

        /// <summary>
        /// Sends the forward request to the upstream host.
        /// </summary>
        /// <returns>An HttpResponseMessage </returns>
        public async Task<HttpResponseMessage> Send()
        {
            try
            {
                return await _httpClient.SendAsync(
                    UpstreamRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    HttpContext.RequestAborted);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is IOException)
            {
                return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
            }
            catch (OperationCanceledException)
            {
                // Happens when Timeout is low and upstream host is not reachable.
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            catch (HttpRequestException ex)
                when (ex.InnerException is IOException || ex.InnerException is SocketException)
            {
                // Happens when server is not reachable
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}