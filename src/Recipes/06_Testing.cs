using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyKit.Testing;

namespace ProxyKit.Recipes
{
    public class Testing
    {
        public async Task Run(CancellationToken cancellationToken)
        {
            var router = new RoutingMessageHandler();

            // Build Proxy TestServer
            var proxyWebHostBuilder = new WebHostBuilder()
                .UseStartup<ProxyStartup>()
                .ConfigureServices(services =>
                {
                    // configure proxy to forward requests to the router
                    services.AddSingleton<Func<HttpMessageHandler>>(() => router);
                })
                .UseUrls("http://localhost:5000");
            var proxyTestServer = new TestServer(proxyWebHostBuilder);
            router.AddHandler("localhost", 5000, proxyTestServer.CreateHandler());

            // Build Host1 TestServer
            var host1WebHostBuilder = new WebHostBuilder()
                .UseStartup<HostStartup>()
                .UseSetting("hostname", "HOST 1")
                .UseUrls("http://localhost:5001");
            var host1TestServer = new TestServer(host1WebHostBuilder);
            router.AddHandler("localhost", 5001, host1TestServer.CreateHandler());

            // Build Host2 TestServer
            var host2WebHostBuilder = new WebHostBuilder()
                .UseStartup<HostStartup>()
                .UseSetting("hostname", "HOST 2")
                .UseUrls("http://localhost:5002");
            var host2TestServer = new TestServer(host2WebHostBuilder);
            router.AddHandler("localhost", 5002, host2TestServer.CreateHandler());

            // Get HttpClient make a request to the proxy

            var httpClient = new HttpClient(router);
            var response = await httpClient.GetAsync("http://localhost:5000/", cancellationToken);
        }

        // The Proxy Startup that has a handler (the routing handler) injected.
        public class ProxyStartup
        {
            private readonly Func<HttpMessageHandler> _createHandler;

            public ProxyStartup(Func<HttpMessageHandler> createHandler)
            {
                _createHandler = createHandler;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                // Set the primary handler to the injected handler 
                // so upstream requests are sent to the router
                services.AddProxy(httpClientBuilder 
                    => httpClientBuilder.ConfigurePrimaryHttpMessageHandler(_createHandler));
            }

            public void Configure(IApplicationBuilder app)
            {
                var roundRobin = new RoundRobin
                {
                    "http://localhost:5001",
                    "http://localhost:5002"
                };

                app.RunProxy(
                    context =>
                    {
                        var host = roundRobin.Next();

                        return context
                            .ForwardTo(host)
                            .AddXForwardedHeaders()
                            .Send();
                    });
            }
        }

        public class HostStartup
        {
            public void ConfigureServices(IServiceCollection services)
            { }

            public void Configure(IApplicationBuilder app, IConfiguration config)
            {
                app.UseXForwardedHeaders(new ForwardedHeadersOptions
                {
                    AllowedHosts = new List<string> { "localhost" },
                    ForwardedHeaders = ForwardedHeaders.All
                });

                app.Run(async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync($"Hi from Host {config.GetValue<string>("hostname")}\n");
                    await context.Response.WriteAsync($"RequestUrl={context.Request.GetEncodedUrl()}\n");
                    await context.Response.WriteAsync($"PathBase={context.Request.PathBase}\n");
                    foreach (var requestHeader in context.Request.Headers)
                    {
                        await context.Response.WriteAsync(
                            $"{requestHeader.Key}={string.Join(",", requestHeader.Value)}\n");
                    }
                });
            }
        }
    }
}
