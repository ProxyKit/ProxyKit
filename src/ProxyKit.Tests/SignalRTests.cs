using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class SignalRTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public SignalRTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Can_connect_via_proxy()
        {
            using (var signalRServer = BuildSignalRServerOnRandomPort())
            {
                await signalRServer.StartAsync();
                var signalRPort = signalRServer.GetServerPort();

                using (var proxyServer = BuildWebSocketProxyServerOnRandomPort(signalRPort))
                {
                    await proxyServer.StartAsync();
                    var proxyPort = proxyServer.GetServerPort();

                    var directConnection = new HubConnectionBuilder()
                        .WithUrl($"http://localhost:{signalRPort}/ping")
                        .Build();
                    directConnection.Closed += async error =>
                    {
                        //await connection.StartAsync();
                    };
                    await directConnection.StartAsync();

                    var proxyConnection = new HubConnectionBuilder()
                        .WithUrl($"http://localhost:{proxyPort}/ping")
                        .Build();
                    proxyConnection.Closed += async error =>
                    {
                        //await connection.StartAsync();
                    };
                    await proxyConnection.StartAsync();
                }
            }
        }

        public static IWebHost BuildSignalRServerOnRandomPort() =>
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseStartup<SignalRServerStartup>()
                .Build();

        public static IWebHost BuildWebSocketProxyServerOnRandomPort(int port) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseSetting("port", port.ToString())
                .UseStartup<WebSocketProxyStartup>()
                .Build();

        public class SignalRServerStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(
                        "all",
                        policy => policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod());
                });

                services.AddSignalR();
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                app.UseCors("all");
                app.UseXForwardedHeaders();

                app.UseSignalR(routes =>
                {
                    routes.MapHub<Ping>("/ping");
                });
            }

            public class Ping : Hub
            {
                public Task PingAll()
                {
                    return Clients.All.SendAsync("OnPing");
                }
            }
        }

        public class WebSocketProxyStartup
        {
            private readonly IConfiguration _config;

            public WebSocketProxyStartup(IConfiguration config)
            {
                _config = config;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app, IServiceProvider sp)
            {
                var port = _config.GetValue("port", 0);
                app.UseWebSocketProxy(new Uri($"ws://localhost:{port}"));
                app.RunProxy(context => context
                    .ForwardTo(new Uri($"http://localhost:{port}"))
                    .AddXForwardedHeaders()
                    .Send());
            }
        }
    }
}
