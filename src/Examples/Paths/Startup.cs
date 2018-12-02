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
                requestContext => requestContext.ForwardTo("http", new HostString("localhost", 5001), "foo"),
                prepareRequestContext => prepareRequestContext.ApplyXForwardedHeader());

            app.RunProxy("/app2", requestContext => requestContext.ForwardTo(new Uri("http://localhost:5002/bar/")));

            app.RunProxy("/app3", requestContext => requestContext.ForwardTo("http://localhost:5003/"));

            // Can return a status code instead. Leverage AspNetCores StatusCodePages middleware to customize
            // the response body.
            app.RunProxy("/app4", requestContext => HttpStatusCode.ServiceUnavailable);

            // default handler (optional). Alternatively can just have an MVC application here.
            app.RunProxy(
                requestContext => requestContext.ForwardTo("http://localhost:5000/"),
                prepareRequestContext => prepareRequestContext.ApplyXForwardedHeader());
        }
    }
}
