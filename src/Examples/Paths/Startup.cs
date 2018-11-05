using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.Paths
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.RunProxy(
                "/app1",
                requestContext => requestContext.ForwardTo("http", new HostString("localhost", 5001), "foo"),
                prepareRequestContext => prepareRequestContext.ApplyForwardedHeader());

            app.RunProxy("/app2", requestContext => requestContext.ForwardTo(new Uri("http://localhost:5002/bar/")));

            app.RunProxy("/app3", requestContext => requestContext.ForwardTo("http://localhost:5003/"));

            // default
            app.RunProxy(
                requestContext => requestContext.ForwardTo("http://localhost:5000/"),
                prepareRequestContext => prepareRequestContext.ApplyForwardedHeader());
        }
    }
}
