// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            Action<SharedProxyOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.Configure(configureOptions);

            services.AddHttpClient("ProxyKit")
                .ConfigureHttpClient((sp, c) => sp.GetRequiredService<IOptions<SharedProxyOptions>>().Value.ConfigureHttpClient?.Invoke(sp, c))
                .ConfigurePrimaryHttpMessageHandler(sp =>
                    // Get the message handler from the options
                    sp.GetRequiredService<IOptions<SharedProxyOptions>>().Value.GetMessageHandler?.Invoke() 
                    // Or construct one with default settings
                    ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });

            return services;
        }

        public static IServiceCollection AddProxy(
            this IServiceCollection services)
        {
            return AddProxy(services, _ => { });
        }
    }
}
