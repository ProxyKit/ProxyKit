using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CacheCow.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class CacheCowTests
    {
        [Fact]
        public async Task Should_return_cached_item()
        {
            using (var server = TestStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "4")
                    .UseStartup<TestCachingStartup>()))
                {
                    var client = testServer.CreateClient();

                    var response = await client.GetAsync("/realServer/normal");
                    response.Headers
                        .GetValues("x-cachecow-client")
                        .Single()
                        .ShouldContain("not-cacheable=true;did-not-exist=true");

                    response = await client.GetAsync("/realServer/cachable");
                    response.Headers
                        .GetValues("x-cachecow-client")
                        .Single()
                        .ShouldContain("did-not-exist=true");

                    response = await client.GetAsync("/realServer/cachable");
                    response.Headers
                        .GetValues("x-cachecow-client")
                        .Single()
                        .ShouldContain("did-not-exist=false");
                }
            }
        }


        public class TestCachingStartup
        {
            private readonly IConfiguration _config;

            public TestCachingStartup(IConfiguration config)
            {
                _config = config;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                var timeout = _config.GetValue("timeout", 60);
                var cacheStore = new InMemoryCacheStore(TimeSpan.FromMinutes(1));
                services.AddProxy(options =>
                {
                    options.ConfigureHttpClient =
                        (serviceProvider, client) => client.Timeout = TimeSpan.FromSeconds(timeout);
                    options.CreateMessageHandler = () => new CachingHandler(cacheStore)
                    {
                        InnerHandler = new HttpClientHandler {AllowAutoRedirect = false, UseCookies = false}
                    };
                });
            }

            public void Configure(IApplicationBuilder app, IServiceProvider sp)
            {
                app.UseXForwardedHeaders();

                var port = _config.GetValue("Port", 0);
                if (port != 0)
                {
                    app.Map("/realServer", appInner =>
                        appInner.RunProxy(context => context
                            .ForwardTo("http://localhost:" + port + "/")
                            .Send()));
                }

            }
        }
    }
}
