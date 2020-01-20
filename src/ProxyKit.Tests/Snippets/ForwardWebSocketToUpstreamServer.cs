using System;
using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class ForwardWebSocketToUpstreamServer
{
    #region ForwardWebSocketToUpstreamServer
    public void Configure(IApplicationBuilder app)
    {
        app.UseWebSockets();
        app.UseWebSocketProxy(
            context => new Uri("ws://upstream-host:80/"),
            options => options.AddXForwardedHeaders());
    }
    #endregion
}