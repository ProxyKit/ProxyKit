using System;
using System.Linq;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class CacheCowTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CacheCowTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_return_cached_item()
        {
            using (var server = RealStartup.BuildKestrelBasedServerOnRandomPort(_testOutputHelper))
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "4")
                    .UseStartup<TestCachingStartup>()))
                {
                    var client = testServer.CreateClient();

                    var response = await client.GetAsync("/realserver/normal");
                    response.Headers
                        .GetValues("x-cachecow-client")
                        .Single()
                        .ShouldContain("not-cacheable=true;did-not-exist=true");

                    response = await client.GetAsync("/realserver/cachable");
                    response.Headers
                        .GetValues("x-cachecow-client")
                        .Single()
                        .ShouldContain("did-not-exist=true");

                    response = await client.GetAsync("/realserver/cachable");
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
                // Add the CacheCow services and types to the service collection
                // Note: using in-memory implementations here for brevity.
                // See https://github.com/aliostad/CacheCow for full documentation.
                services.AddSingleton<ICacheStore>(new InMemoryCacheStore(TimeSpan.FromMinutes(1)));
                services.AddSingleton<IVaryHeaderStore>(new InMemoryVaryHeaderStore());
                services.AddTransient<CachingHandler>();

                services.AddProxy(httpClientBuilder =>
                {
                    httpClientBuilder.AddHttpMessageHandler<CachingHandler>();
                });
            }

            public void Configure(IApplicationBuilder app, IServiceProvider sp)
            {
                app.UseForwardedHeadersWithPathBase();

                var port = _config.GetValue("Port", 0);
                if (port != 0)
                {
                    app.Map("/realserver", appInner =>
                        appInner.RunProxy(context => context
                            .ForwardTo("http://localhost:" + port + "/")
                            .Send()));
                }

            }
        }
    }
}
