using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class SignalROnPath
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                // must first be able to handle incoming websocket requests
                app.UseWebSockets();

                app.Map("/app", signalrApp =>
                {
                    // SignalR, as part of it's protocol, needs both http and ws traffic
                    // to be forwarded to the servers hosting signalr hubs.
                    signalrApp.UseWebSocketProxy(context => new Uri("ws://upstream-host:80"));
                    signalrApp.RunProxy(context => context
                        .ForwardTo(new Uri($"http://upstream-host:80"))
                        .AddXForwardedHeaders()
                        .Send());
                });
            }
        }
    }
}