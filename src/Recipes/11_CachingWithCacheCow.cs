using System;
using CacheCow.Client;
using CacheCow.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class CachingWithCacheCow : ExampleBase<CachingWithCacheCow.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                // Add the CacheCow stores to the service collection
                // Note: using in-memory implementations here for brevity.
                // See https://github.com/aliostad/CacheCow for full documentation.
                services.AddSingleton<ICacheStore>(new InMemoryCacheStore(TimeSpan.FromMinutes(1)));
                services.AddSingleton<IVaryHeaderStore>(new InMemoryVaryHeaderStore());

                // We need to register the CacheCow caching handler. This will
                // be used by the ProxyKit's HttpClient whose constructor
                // takes the ICacheStore and IVaryHeaderStore implementations.
                services.AddTransient<CachingHandler>();

                services.AddProxy(httpClientBuilder =>
                {
                    // Tell ProxyKit to use the CachingHandler in it's HttpClient.
                    httpClientBuilder.AddHttpMessageHandler<CachingHandler>();
                });
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy(context => context
                    .ForwardTo("http://localhost:5001")
                    .CopyXForwardedHeaders() // copies the headers from the incoming requests
                    .AddXForwardedHeaders() // adds the current proxy proto/host/for/pathbase to the X-Forwarded headers
                    .Send());
            }
        }
    }
}
