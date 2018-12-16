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
        private readonly IHttpClientFactory _httpClientFactory;

        public ForwardContext(
            HttpContext httpContext,
            HttpRequestMessage request,
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            HttpContext = httpContext;
            Request = request;
        }

        public HttpContext HttpContext { get; }

        public HttpRequestMessage Request { get; }

        public async Task<HttpResponseMessage> Handle()
        {
            var httpClient = _httpClientFactory.CreateClient("ProxyKit");
            try
            {
                return await httpClient.SendAsync(
                    Request,
                    HttpCompletionOption.ResponseHeadersRead,
                    HttpContext.RequestAborted);
            }
            catch (TaskCanceledException ex)
            {
                // Task cancelled exceptions can happen when either client disconnects before server has time to respond 
                // or when the proxy request times out. 
                if (RequestHasTimedOut(ex))
                {
                    return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
                }

                throw;
            }
            catch (OperationCanceledException)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            catch (HttpRequestException ex)
            {
                if (UpstreamIsUnavailable(ex))
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }

                throw;
            }
        }

        private static bool RequestHasTimedOut(OperationCanceledException ex)
            => ex.InnerException is IOException;

        private static bool UpstreamIsUnavailable(HttpRequestException ex)
            => ex.InnerException is IOException || ex.InnerException is SocketException;
    }
}