using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using ProxyKit.DownstreamServer;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class Tests
    {
        [Fact]
        public async Task CanGetRoot()
        {
            var fixture = new ProxyFixture();

            var httpClient = fixture.ProxyServer.CreateClient();

            var response = await httpClient.GetAsync("/");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public class ProxyFixture
    {
        public ProxyFixture()
        {
            var downstreamHostBuilder = new WebHostBuilder()
                .UseStartup<DownstreamStartup>();

            var downstreamServer = new TestServer(downstreamHostBuilder);

            var downstreamHandler = downstreamServer.CreateHandler();

            var proxyBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddProxy(sharedOptions =>
                    {
                        sharedOptions.MessageHandler = downstreamHandler;
                    });
                })
                .Configure(app =>
                {
                    app.RunProxy(
                        requestContext => requestContext.ForwardTo("http://downstreamserver:5000/"),
                        prepareRequestContext => prepareRequestContext.ApplyForwardedHeader());
                });

            ProxyServer = new TestServer(proxyBuilder);
        }

        public TestServer ProxyServer { get; }
    }
}
