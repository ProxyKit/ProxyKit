using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ProxyKit
{
    public class ForwardContext
    {
        private readonly HttpClient _httpClient;
        ProxyOptions proxyOptions;
        internal ForwardContext(
            HttpContext httpContext,
            HttpRequestMessage upstreamRequest,
            HttpClient httpClient,
            IOptionsMonitor<ProxyOptions> proxyOptions)
        {
            _httpClient = httpClient;
            HttpContext = httpContext;
            UpstreamRequest = upstreamRequest;
            this.proxyOptions = proxyOptions.CurrentValue;
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
                var resultMessage = await _httpClient.SendAsync(
                    UpstreamRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    HttpContext.RequestAborted);

                if (this.proxyOptions.RewriteResponseLocationHeader)
                {
                    var locationHeader = resultMessage.Headers.FirstOrDefault(q => "Location".Equals(q.Key, StringComparison.OrdinalIgnoreCase));
                    var locationHeaderValue = locationHeader.Value?.FirstOrDefault();

                    if (locationHeaderValue != null)
                    {
                        var uri = new Uri(locationHeaderValue, UriKind.RelativeOrAbsolute);
                        if (uri.IsAbsoluteUri)
                        {
                            // Check if the Location comes from same Origin
                            var upstreamUri = this.UpstreamRequest.RequestUri;
                            if (upstreamUri.GetLeftPart(UriPartial.Authority) == uri.GetLeftPart(UriPartial.Authority))
                            {
                                var request = this.HttpContext.Request;
                                var replacedUrl = $"{request.Scheme}://{request.Host.ToString()}{uri.PathAndQuery}";

                                resultMessage.Headers.Remove("Location");
                                resultMessage.Headers.Add("Location", replacedUrl);
                            }
                        }
                    }
                }

                return resultMessage;
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