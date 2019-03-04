using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var result = new HttpClientHandler
                    {
                        AllowAutoRedirect = false,
                        UseCookies = false
                    };

                    var proxyOptions = sp.GetService<IOptionsMonitor<ProxyOptions>>().CurrentValue;
                    if (proxyOptions.IgnoreSSLCertificate)
                    {
                        result.ServerCertificateCustomValidationCallback = (_1, _2, _3, _4) => true;
                    }

                    return result;
                });

            configureHttpClientBuilder?.Invoke(httpClientBuilder);

            configureOptions = configureOptions ?? (_ => { });
            services
                .Configure(configureOptions)
                .AddOptions<ProxyOptions>();
            return services;
        }
    }
}
