using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using System.Net.WebSockets;
using System.Net.Sockets;
using Microsoft.AspNetCore.WebSockets;


namespace WebSockerReceive
{
    public class Startup
    {

        private static async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        public static void ConfigureServices(IServiceCollection services)
        {
        }

        public static void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
					Console.WriteLine("Message received");
					
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                        await Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello World");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5121")
                .Build();

            host.Run();
        }
    }
}