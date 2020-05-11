using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit.Infra;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class AzureTests : IAsyncLifetime
    {
        private readonly IWebHost _proxy;
        private int _port;

        public AzureTests(ITestOutputHelper outputHelper)
        {
            _proxy = new WebHostBuilder()
                .UseStartup<ProxyStartup>()
                .UseKestrel()
                .UseUrls("http://*:0")
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new XunitLoggerProvider(outputHelper, "Proxy"));
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();
        }

        [Fact]
        public async Task Can_proxy_azure_app_service()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{_port}")
            };

            var response = await client.GetAsync("");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task When_body_included_with_transfer_encoding_should_get_bad_gateway()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{_port}")
            };

            var request = new HttpRequestMessage(HttpMethod.Get, "")
            {
                // This enforces using Transfer-Encoding: chunked
                Content = new PushStreamContent(async (stream, httpContext, transPortContext) =>
                {
                    var buffer = new byte[100];
                    await stream.WriteAsync(buffer);
                    await stream.FlushAsync();
                    stream.Dispose();
                })
            };
            var response = await client.SendAsync(request);

            response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        }

        protected class ProxyStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                // my blog is currently hosted on azure. If it move this will need to be changed.
                app.RunProxy(context => context
                    .ForwardTo("http://dhickey.ie")
                    .Send());
            }
        }

        public async Task InitializeAsync()
        {
            await _proxy.StartAsync();

            _port = _proxy.GetServerPort();
        }

        public async Task DisposeAsync()
        {
            await _proxy.StopAsync();
        }
    }

}
