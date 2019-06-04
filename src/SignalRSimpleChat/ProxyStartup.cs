using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipe.SignalRSimpleChat
{
    public class ProxyStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
            // must first be able to handle incoming websocket requests
            app.UseWebSockets();

            // SignalR, as part of it's protocol, needs both http and ws traffic
            // to be forwarded to the servers hosting signalr hubs.
            app.Map("/subpath", appInner =>
            {
                appInner.UseWebSocketProxy(context => new Uri("ws://localhost:5001/subpath/"));
                appInner.RunProxy(context => context
                    .ForwardTo("http://localhost:5001/subpath/")
                .AddXForwardedHeaders()
                    .Send());
            });
        }
    }
}