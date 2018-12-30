using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UpstreamServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WebHost
                .CreateDefaultBuilder<Startup>(Array.Empty<string>())
                .UseUrls("http://+:5002")
                .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                .Build()
                .Run();
        }

        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {}

            public void Configure(IApplicationBuilder app)
            {
                app.Run(async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync($"Hello, ProxyKit!\n");
                });
            }
        }
    }
}
