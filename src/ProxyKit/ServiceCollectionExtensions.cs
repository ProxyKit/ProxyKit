using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProxy(
            this IServiceCollection services,
            Action<IHttpClientBuilder> configureHttpClientBuilder = null,
            Action<ProxyOptions> configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var httpClientBuilder = services
                .AddHttpClient<ProxyKitClient>()
                .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false
                });

            configureHttpClientBuilder?.Invoke(httpClientBuilder);

            configureOptions = configureOptions ?? (_ => { });
            services.Configure(configureOptions);
            services.AddTransient<ProxyOptions>();
            return services;
        }
    }
}
