using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit.Infra;
using Shouldly;
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
            using (var signalRServer = BuildSignalRServerOnRandomPort(_outputHelper))
            {
                await signalRServer.StartAsync();
                var signalRPort = signalRServer.GetServerPort();

                using (var proxyServer = BuildWebSocketProxyServerOnRandomPort(signalRPort, _outputHelper))
                {
                    await proxyServer.StartAsync();
                    var proxyPort = proxyServer.GetServerPort();

                    // Connection directly to SignalR Server
                    var directConnection = new HubConnectionBuilder()
                        .WithUrl($"http://localhost:{signalRPort}/ping")
                        .ConfigureLogging(logging => logging
                            .AddDebug()
                            .AddProvider(new XunitLoggerProvider(_outputHelper, "connection-direct")))
                        .Build();
                    directConnection.Closed += async error =>
                    {
                        _outputHelper.WriteLine("connection-direct error: " + error.ToString());
                    };
                    await directConnection.StartAsync();

                    // Callback when On
                    var messageRecieved = new TaskCompletionSource<bool>();
                    directConnection.On("OnPing", () =>
                    {
                        messageRecieved.SetResult(true);
                    });

                    // Connect to SignalR Server via proxy
                    var proxyConnection = new HubConnectionBuilder()
                        .WithUrl($"http://localhost:{proxyPort}/ping")
                        .ConfigureLogging(logging => logging
                            .AddDebug()
                            .AddProvider(new XunitLoggerProvider(_outputHelper, "connection-proxy")))
                        .Build();
                    proxyConnection.Closed += async error =>
                    {
                        _outputHelper.WriteLine(error.ToString());
                    };
                    await proxyConnection.StartAsync();

                    // Send message to all clients
                    await proxyConnection.InvokeAsync("PingAll");

                    messageRecieved.Task.Wait(TimeSpan.FromSeconds(3)).ShouldBeTrue();
                    messageRecieved.Task.Result.ShouldBeTrue();
                }
            }
        }

        public static IWebHost BuildSignalRServerOnRandomPort(ITestOutputHelper outputHelper) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .ConfigureLogging(logging => logging
                    .AddDebug()
                    .AddProvider(new XunitLoggerProvider(outputHelper, "Upstream")))
                .UseStartup<SignalRServerStartup>()
                .Build();

        public static IWebHost BuildWebSocketProxyServerOnRandomPort(int port, ITestOutputHelper outputHelper) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseSetting("port", port.ToString())
                .ConfigureLogging(logging => logging
                    .AddDebug()
                    .AddProvider(new XunitLoggerProvider(outputHelper, "Proxy")))
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
                app.UseWebSockets();
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

                app.UseWebSockets();
                app.UseWebSocketProxy(_ => new Uri($"ws://localhost:{port}"));
                app.RunProxy(context => context
                    .ForwardTo(new Uri($"http://localhost:{port}"))
                    .AddXForwardedHeaders()
                    .Send());
            }
        }
    }
}
