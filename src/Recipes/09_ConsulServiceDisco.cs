using System;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class ConsulServiceDiscovery
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
                services.AddSingleton<ConsulClient>();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy("api/foo", async context =>
                {
                    var consulClient = context.RequestServices.GetRequiredService<ConsulClient>();
                    var service = await consulClient.Catalog.Service("foo-service");
                    var upstreamHost = new Uri($"https://{service.Response[0].Address}:{service.Response[0].ServicePort}");
                    return await context
                        .ForwardTo(upstreamHost)
                        .AddXForwardedHeaders()
                        .Send();
                });

                app.RunProxy("api/bar", async context =>
                {
                    var consulClient = context.RequestServices.GetRequiredService<ConsulClient>();
                    var service = await consulClient.Catalog.Service("bar-service");
                    var upstreamHost = new Uri($"https://{service.Response[0].Address}:{service.Response[0].ServicePort}");
                    return await context
                        .ForwardTo(upstreamHost)
                        .AddXForwardedHeaders()
                        .Send();
                });
            }
        }
    }
}
