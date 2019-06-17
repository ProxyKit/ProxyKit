using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class WebSockets
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

                // websockets proxy is non-terminating
                app.UseWebSocketProxy(
                    context => new Uri("ws://upstream-host:80/"),
                    // optionally add X-ForwardedHeaders to websocket client.
                    options => options.AddXForwardedHeaders());
            }
        }
    }
}
