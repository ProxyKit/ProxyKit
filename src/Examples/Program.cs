using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyConsole;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var start1Async = WebHost
                .CreateDefaultBuilder<HostStartup>(args)
                .UseUrls("http://localhost:5001")
                .UseSetting("hostname", "host 1")
                .Build()
                .StartAsync();

            var start2Async = WebHost
                .CreateDefaultBuilder<HostStartup>(args)
                .UseUrls("http://localhost:5002")
                .UseSetting("hostname", "host 2")
                .Build()
                .StartAsync();

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, __) => cts.Cancel();
            new Menu()
                .Add(
                    "Basic Forwarding",
                    () => new Basic().Run(cts.Token))
                .Add(
                    "Path Forwarding",
                    () => new Paths().Run(cts.Token))
                .Add(
                    "Round Robin",
                    () => new RoundRobinLoadBalancer().Run(cts.Token))
                .Add(
                    "Testing",
                    () => new Testing().Run().GetAwaiter().GetResult())
                .Display();
        }

        public class HostStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {}

            public void Configure(IApplicationBuilder app, IConfiguration config)
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    AllowedHosts = new List<string> {"localhost"},
                    ForwardedHeaders = ForwardedHeaders.All
                });

                app.Use((context, next) =>
                {
                    if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBase))
                    {
                        context.Request.PathBase = new PathString(pathBase);
                    }
                    return next();
                });

                app.Run(async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync($"Hi from Host {config.GetValue<string>("hostname")}\n");
                    await context.Response.WriteAsync($"RequestUrl={context.Request.GetEncodedUrl()}\n");
                    await context.Response.WriteAsync($"PathBase={context.Request.PathBase}\n");
                    foreach (var requestHeader in context.Request.Headers)
                    {
                        await context.Response.WriteAsync(
                            $"{requestHeader.Key}={string.Join(",", requestHeader.Value)}\n");
                    }
                });
            }
        }
    }
}
