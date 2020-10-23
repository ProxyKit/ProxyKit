using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class HttpContextExtensions
    {
        /// <summary>
        ///     Forward the request to the specified upstream host.
        /// </summary>
        /// <param name="context">The HttpContext</param>
        /// <param name="upstreamHost">The upstream host to forward the requests
        /// to.</param>
        /// <returns>A <see cref="ForwardContext"/> that represents the
        /// forwarding request context.</returns>
        public static ForwardContext ForwardTo(this HttpContext context, UpstreamHost upstreamHost)
        {
            var uri = new Uri(UriHelper.BuildAbsolute(
                upstreamHost.Scheme,
                upstreamHost.Host,
                upstreamHost.PathBase,
                context.Request.Path,
                context.Request.QueryString));

            var request = context.Request.CreateProxyHttpRequest();
            request.Headers.Host = uri.Authority;
            request.RequestUri = uri;

            IHttpClientFactory httpClientFactory;
            try
            {
                httpClientFactory = context
                    .RequestServices
                    .GetRequiredService<IHttpClientFactory>();
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"{exception.Message} Did you forget to call services.AddProxy()?",
                    exception);
            }

            var httpClient = httpClientFactory.CreateClient(ServiceCollectionExtensions.ProxyKitHttpClientName);

            return new ForwardContext(context, request, httpClient);
        }

        private static HttpRequestMessage CreateProxyHttpRequest(this HttpRequest request)
        {
            var requestMessage = new HttpRequestMessage();

            // The presence of a message-body in a request is signaled by the
            // inclusion of a Content-Length or Transfer-Encoding header field in
            // the request's message-headers. https://tools.ietf.org/html/rfc2616 4.3 MessageBody
            if (request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding"))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers *except* x-forwarded-* headers.
            foreach (var header in request.Headers)
            {
                if (header.Key.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }


#if NETSTANDARD2_0
            // HACK: Attempting to send a malformed User-Agent will throw from with HttpClient
            // https://github.com/damianh/ProxyKit/issues/53
            // https://github.com/dotnet/corefx/issues/34933
            try
            {
                requestMessage.Headers.TryGetValues("User-Agent", out var _);
            }
            catch (IndexOutOfRangeException)
            {
                requestMessage.Headers.Remove("User-Agent");
            }
#endif

            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
    }
}
