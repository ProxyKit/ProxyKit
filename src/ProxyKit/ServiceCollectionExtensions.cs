using System;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class ServiceCollectionExtensions
    {
        internal const string ProxyKitHttpClientName = "ProxyKitClient";

        public static IServiceCollection AddProxy(
            this IServiceCollection services,
            Action<IHttpClientBuilder> configureHttpClientBuilder = null,
            Action<ProxyOptions> configureOptions = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var httpClientBuilder = services
                .AddHttpClient(ProxyKitHttpClientName)
                .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false
                });

            configureHttpClientBuilder?.Invoke(httpClientBuilder);

            configureOptions ??= (_ => { });
            services
                .Configure(configureOptions)
                .AddOptions<ProxyOptions>();
            return services;
        }
    }
}
