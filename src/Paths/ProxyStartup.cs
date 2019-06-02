using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipe.Simple
{
    public class ProxyStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Forwards requests from /app1 to upstream host http://localhost:5001/foo/
            app.Map("/app1", app1 =>
            {
                app1.RunProxy(context => context
                    .ForwardTo("http://localhost:5001/foo/")
                    .AddXForwardedHeaders()
                    .Send());
            });

            // Forwards requests from /app2 to upstream host http://localhost:5002/bar/
            app.Map("/app2", app2 =>
            {
                app2.RunProxy(context => context
                    .ForwardTo("http://localhost:5002/bar/")
                    .AddXForwardedHeaders()
                    .Send());
            });

            app.Run(async (context) =>
            {
                var html = new StringBuilder("<!DOCTYPE html><html><body><h2>Proxy Host</h2>");
                html.Append("<p><a href=\"/app1\">App 1</a><br>");
                html.Append("<a href=\"/app2\">App 2</a></p>");
                html.Append("</body></html>");
                await context.Response.WriteAsync(html.ToString());
            });
        }
    }
}