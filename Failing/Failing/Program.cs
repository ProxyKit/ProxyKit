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

using ProxyKit;

namespace Sample.Proxy
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
            services.AddProxy();
        }

        public static void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();

            app.UseWebSocketProxy(
               context =>
               {
                   return new Uri("ws://localhost:5121/ws");
               },
               options => options.AddXForwardedHeaders());

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello World");
            });
        }

        public static async void Send(ClientWebSocket webSocket, byte[] cmd)
        {
            byte[] vs = new byte[1000];

            ArraySegment<byte> buffer = new ArraySegment<byte>(vs);

            await webSocket.SendAsync(new ArraySegment<byte>(cmd), WebSocketMessageType.Text, true, CancellationToken.None);

            Console.WriteLine("Message Sent");

            WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            Console.WriteLine(System.Text.Encoding.ASCII.GetString(buffer.Array, 0, result.Count));
        }

        public static void Start()
        {
            while (true)
            {
                ClientWebSocket webSocket = new ClientWebSocket();

                webSocket.ConnectAsync(new Uri("ws://localhost:5050/ws"), CancellationToken.None);

                byte[] cmd = System.Text.Encoding.ASCII.GetBytes("Hello");

                while (true)
                {
                    if (webSocket.State == WebSocketState.Open)
                        Send(webSocket, cmd);
                    else if (webSocket.State == WebSocketState.Connecting)
                        Thread.Sleep(50);
                    else
                        break;
                }
            }
            
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5050")
                .Build();

            // Thread thread = new Thread(Start);

            // thread.Start();

            host.Run();
        }
    }
}