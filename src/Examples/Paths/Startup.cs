using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.Paths
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy(); //Required to add this.
        }

        public void Configure(IApplicationBuilder app)
        {
            // Can return the destination URI in three different ways.
            app.RunProxy(
                "/app1",
                (context, handle) =>
                {
                    context.ForwardTo("http", new HostString("localhost", 5001), "foo");
                    context.ApplyXForwardedHeaders();
                    return handle();
                });

            app.RunProxy("/app2",
                (context, handle) =>
                {
                    context.ForwardTo(new Uri("http://localhost:5002/bar/"));
                    context.ApplyXForwardedHeaders();
                    return handle();
                });
        }
    }
}
