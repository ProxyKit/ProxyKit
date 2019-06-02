using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipe.Simple
{
    public class UpstreamHostStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {}

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Run(async (context) =>
            {
                var html = new StringBuilder("<!DOCTYPE html><html><body><h2>Hello from upstream host!</h2>");
                html.Append("<table><tr><th align=\"left\">Header</th><th align=\"left\">Value</th><tr>");
                html.Append($"<tr><td>Host</td><td>{context.Request.Host}</td></tr>");
                html.Append($"<tr><td>Path</td><td>{context.Request.Path}</td></tr>");
                foreach (var (key, value) in context.Request.Headers)
                {
                    html.Append($"<tr><td>{key}</td><td>{value}</td></tr>");
                }
                html.Append("</table></body></html>");

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(html.ToString());
            });
        }
    }
}