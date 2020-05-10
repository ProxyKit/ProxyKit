using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProxyKit.RoutingHandler;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class TestServerAcceptanceTests: AcceptanceTestsBase
    {
        private TestServer _upstreamTestServer;
        private TestServer _proxyTestServer;
        private int _proxyPort;

        public TestServerAcceptanceTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {}

        protected override int ProxyPort => _proxyPort;

        protected override HttpClient CreateClient()
        {
            var client = _proxyTestServer.CreateClient();
            client.BaseAddress = new Uri($"http://localhost:{_proxyPort}/");
            return client;
        }

        public override Task InitializeAsync()
        {
            var router = new RoutingMessageHandler();
            _upstreamTestServer = new TestServer(UpstreamBuilder);
            _proxyPort = 81;

            var proxyBuilder = ProxyBuilder
                .UseSetting("port", _proxyPort.ToString())
                .ConfigureTestServices(services =>
                {
                    services.AddProxy(c =>
                    {
                        c.ConfigurePrimaryHttpMessageHandler(() => router);
                        c.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(1));
                    });
                });

            _proxyTestServer = new TestServer(proxyBuilder);

            router.AddHandler(new Origin("localhost", _proxyPort), _upstreamTestServer.CreateHandler());

            return Task.CompletedTask;
        }

        public override Task DisposeAsync()
        {
            _upstreamTestServer.Dispose();
            _proxyTestServer.Dispose();
            return Task.CompletedTask;
        }
    }
}