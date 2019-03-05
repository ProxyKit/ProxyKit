using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public class RealStartup
    {
        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app)
        {
            var options = new ForwardedHeadersOptions();
            options.AllowedHosts.Add("*");
            options.ForwardedHeaders = ForwardedHeaders.All;
            app.UseXForwardedHeaders(options);

            app.Map("/normal", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Ok");
            }));

            app.Map("/cachable", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Headers.Add("Cache-Control", "max-age=60");
                await ctx.Response.WriteAsync("Ok");
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

            app.Map("/redirect", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 302;
                ctx.Response.Headers.Add("Location", ctx.Request.GetEncodedUrl());
            }));

            app.Map("/ws", a =>
            {
                app.UseWebSockets();
                app.Use(async (context, next) =>
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
        }

        public static IWebHost BuildKestrelBasedServerOnRandomPort()
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseStartup<RealStartup>()
                .Build();
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
    }
}