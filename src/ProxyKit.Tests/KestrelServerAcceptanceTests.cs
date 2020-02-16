using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit.Infra;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class KestrelServerAcceptanceTests: AcceptanceTestsBase
    {
        private IWebHost _upstreamServer;
        private IWebHost _proxyServer;
        private int _proxyPort;

        public KestrelServerAcceptanceTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {}

        protected override int ProxyPort => _proxyPort;

        protected override HttpClient CreateClient()
        {
            _proxyPort = _proxyServer.GetServerPort();
;           return new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{_proxyPort}")
            };
        }

        [Fact]
        public async Task When_upstream_host_is_not_running_then_should_get_service_unavailable()
        {
            var client = CreateClient();
            await _upstreamServer.StopAsync();
            
            var response = await client.GetAsync("/normal");
            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        public override async Task InitializeAsync()
        {
            _upstreamServer = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseStartup<UpstreamStartup>()
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new XunitLoggerProvider(OutputHelper, "Upstream"));
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();
            await _upstreamServer.StartAsync();
            
            var port = _upstreamServer.GetServerPort();

            _proxyServer = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseStartup<ProxyStartup>()
                .UseSetting("port", port.ToString())
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new XunitLoggerProvider(OutputHelper, "Proxy"));
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices(services =>
                {
                    services.AddProxy(httpClientBuilder => httpClientBuilder
                        .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(2)));
                })
                .Build();
            await _proxyServer.StartAsync();
        }

        public override async Task DisposeAsync()
        {
            await _proxyServer.StopAsync();
            await _upstreamServer.StopAsync();
        }
    }
}
