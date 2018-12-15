using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProxyKit.Testing;

namespace ProxyKit.Examples
{
    public class Testing
    {
        public async Task Run()
        {
            var router = new RoutingMessageHandler();

            // Build proxy
            var proxyWebHostBuilder = new WebHostBuilder()
                .UseStartup<ProxyStartup>()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<HttpMessageHandler>(router);
                })
                .UseUrls("http://localhost:5000");
            var proxyTestServer = new TestServer(proxyWebHostBuilder);
            router.AddHandler(new Uri("http://localhost:5000"), proxyTestServer.CreateHandler());

            // Build Host1
            var host1WebHostBuilder = new WebHostBuilder()
                .UseStartup<Program.HostStartup>()
                .UseSetting("hostname", "HOST 1")
                .UseUrls("http://localhost:5001");
            var host1TestServer = new TestServer(host1WebHostBuilder);
            router.AddHandler(new Uri("http://localhost:5001"), host1TestServer.CreateHandler());

            // Build Host2
            var host2WebHostBuilder = new WebHostBuilder()
                .UseStartup<Program.HostStartup>()
                .UseSetting("hostname", "HOST 2")
                .UseUrls("http://localhost:5002");
            var host2TestServer = new TestServer(host2WebHostBuilder);
            router.AddHandler(new Uri("http://localhost:5002"), host2TestServer.CreateHandler());

            while (true)
            {
                var httpClient = new HttpClient(router);
                var response = await httpClient.GetAsync("http://localhost:5000/");
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
                Console.ReadLine();
            }
        }

        public class ProxyStartup
        {
            private readonly HttpMessageHandler _handler;

            public ProxyStartup(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy(options 
                    => options.GetMessageHandler= () => _handler);
            }

            public void Configure(IApplicationBuilder app)
            {
                var hosts = new List<string>
                {
                    "http://localhost:5001",
                    "http://localhost:5002"
                };
                var roundRobin = new RoundRobin<string>(hosts);

                app.RunProxy(
                    (context, handle) =>
                    {
                        var host = roundRobin.Next();

                        var forwardContext = context
                            .ForwardTo(host)
                            .ApplyXForwardedHeaders();

                        return handle(forwardContext);
                    });
            }
        }
    }
}
