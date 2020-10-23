using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit.Infra;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ProxyKit
{
    public abstract class AcceptanceTestsBase: IAsyncLifetime
    {
        protected AcceptanceTestsBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;

            UpstreamBuilder = new WebHostBuilder()
                .UseStartup<UpstreamStartup>()
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new XunitLoggerProvider(OutputHelper, "Upstream"));
                    builder.SetMinimumLevel(LogLevel.Debug);
                });

            ProxyBuilder = new WebHostBuilder()
                .UseStartup<ProxyStartup>()
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new XunitLoggerProvider(OutputHelper, "Proxy"));
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
        }

        protected ITestOutputHelper OutputHelper { get; }

        protected abstract int ProxyPort { get; }

        protected abstract HttpClient CreateClient();

        protected CookieContainer CookieContainer { get; } = new CookieContainer();

        protected IWebHostBuilder UpstreamBuilder { get; }

        protected IWebHostBuilder ProxyBuilder { get; }

        [Fact]
        public async Task Can_proxy_request()
        {
            var client = CreateClient();
            var response = await client.GetAsync("/normal");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Can_handle_cookies()
        {
            var client = CreateClient();
            CookieContainer.Add(client.BaseAddress, new Cookie("yummy_cookie", "choco"));
            CookieContainer.Add(client.BaseAddress, new Cookie("tasty_cookie", "strawberry"));
            
            var response = await client.GetAsync("/cookie-count");
            
            var body = await response.Content.ReadAsStringAsync();
            body.ShouldBe("2");
        }

        [Fact]
        public async Task Error_codes_should_be_proxied()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/badrequest");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response = await client.GetAsync("/error");
            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task Redirect_location_header_should_have_correct_uri()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/redirect");

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            response.Headers.Location.ShouldBe(new Uri($"http://localhost:{ProxyPort}/redirect"));
        }

        [Fact(Skip = "Passes in kestrel but fails because of bug in TestServer https://github.com/dotnet/aspnetcore/issues/21677")]
        public async Task When_body_included_with_transfer_encoding_should_get_ok()
        {
            var client = CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/normal")
            {
                // This enforces usage of header Transfer-Encoding: chunked
                Content = new PushStreamContent(async (stream, httpContext, transPortContext) =>
                {
                    var buffer = new byte[100];
                    await stream.WriteAsync(buffer, 0, 100);
                    await stream.FlushAsync();
                    stream.Dispose();
                })
            };
            var response = await client.SendAsync(request);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var readAsByteArrayAsync = await response.Content.ReadAsByteArrayAsync();
            readAsByteArrayAsync.Length.ShouldBe(100);
        }

        public abstract Task InitializeAsync();

        public abstract Task DisposeAsync();

        protected class UpstreamStartup
        {
            public void ConfigureServices(IServiceCollection services)
            { }

            public void Configure(IApplicationBuilder app)
            {
                var options = new ForwardedHeadersOptions();
                options.AllowedHosts.Add("*");
                options.KnownProxies.Add(IPAddress.Loopback);
                options.KnownProxies.Add(IPAddress.IPv6Loopback);
                options.KnownProxies.Add(IPAddress.Parse("::ffff:127.0.0.1"));
                options.ForwardedHeaders = ForwardedHeaders.All;
                app.UseXForwardedHeaders(options);

                app.Map("/normal", a => a.Run(async ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentLength = ctx.Request.ContentLength;
                    var buffer = new byte[1024];
                    var bytesRead = await ctx.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        await ctx.Response.Body.WriteAsync(buffer, 0, bytesRead);
                    }
                }));

                app.Map("/cachable", a => a.Run(async ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Headers.Add("Cache-Control", "max-age=60");
                    await ctx.Response.WriteAsync("Ok");
                }));

                app.Map("/cookie-count", a => a.Run(async ctx =>
                {
                    await ctx.Response.WriteAsync(ctx.Request.Cookies.Count.ToString());
                }));

                app.Map("/badrequest", a => a.Run(async ctx =>
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsync("Nah..");
                }));

                app.Map("/slow", a => a.Run(async ctx =>
                {
                    await Task.Delay(5000);
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.WriteAsync("Ok... i guess");
                }));

                app.Map("/error", a => a.Run(async ctx =>
                {
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.WriteAsync("cute..... BUT IT'S WRONG!");
                }));

                app.Map("/redirect", a => a.Run(ctx =>
                {
                    ctx.Response.StatusCode = 302;
                    ctx.Response.Headers.Add("Location", ctx.Request.GetEncodedUrl());
                    return Task.CompletedTask;
                }));

                app.Map("/ws", wsApp =>
                {
                    wsApp.UseWebSockets();
                    wsApp.Use(async (context, next) =>
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            await Echo(context, webSocket);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    });
                });

                app.Map("/ws-custom", wsApp =>
                {
                    wsApp.UseWebSockets();
                    wsApp.Use(async (context, next) =>
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            await EchoTraceIdHeaderAndQuery(context, webSocket);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    });
                });
            }

            private async Task Echo(HttpContext _, WebSocket webSocket)
            {
                var buffer = new byte[1024 * 4];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }

            private async Task EchoTraceIdHeaderAndQuery(HttpContext context, WebSocket webSocket)
            {
                var buffer = new byte[1024 * 4];
                var header = context.Request.Headers["X-TraceId"];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes("X-TraceId=" + header + context.Request.QueryString.Value));
                    await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }

        protected class ProxyStartup
        {
            private readonly IConfiguration _config;

            public ProxyStartup(IConfiguration config)
            {
                _config = config;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
                services.AddSingleton<ProxyHandler>();
            }

            public void Configure(IApplicationBuilder app)
            {
                var port = _config.GetValue("port", 0);

                app.UseWebSockets();
                app.Map("/ws", appInner =>
                {
                    appInner.UseWebSocketProxy(
                        _ => new Uri($"ws://localhost:{port}/ws/"),
                        options => options.AddXForwardedHeaders());
                });

                app.Map("/ws-custom", appInner =>
                {
                    appInner.UseWebSocketProxy(
                        _ => new Uri($"ws://localhost:{port}/ws-custom/"),
                        options => options.SetRequestHeader("X-TraceId", "123"));
                });

                app.RunProxy<ProxyHandler>();
            }
        }

        private class ProxyHandler : IProxyHandler
        {
            private readonly string _forwardTo;

            public ProxyHandler(IConfiguration config)
            {
                var port = config.GetValue("port", 0);
                _forwardTo = $"http://localhost:{port}/";
            }

            public Task<HttpResponseMessage> HandleProxyRequest(HttpContext context)
            {
                var forwardContext = context
                    .ForwardTo(_forwardTo)
                    .AddXForwardedHeaders();

                return forwardContext.Send();
            }
        }
    }
}