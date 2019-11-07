using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

            var options = context.RequestServices.GetService<IOptions<ProxyOptions>>().Value;
            var request = context.Request.CreateProxyHttpRequest(options);
            request.Headers.Host = uri.Authority;
            request.RequestUri = uri;

            var httpClientFactory = context
                .RequestServices
                .GetRequiredService<IHttpClientFactory>();

            var httpClient = httpClientFactory.CreateClient(ServiceCollectionExtensions.ProxyKitHttpClientName);

            return new ForwardContext(context, request, httpClient);
        }

        private static HttpRequestMessage CreateProxyHttpRequest(this HttpRequest request, ProxyOptions options)
        {
            var requestMessage = new HttpRequestMessage();
            if (options.CopyRequestBodyIf(request))
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

            // HACK: Attempting to send a malformed User-Agent will throw from with HttpClient
            // Remove when .net core 3 is released. Consider supporting netcoreapp2.x with #ifdef
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

            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
    }
}
