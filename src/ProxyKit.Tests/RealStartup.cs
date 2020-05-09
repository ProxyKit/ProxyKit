using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit.Infra;
using Xunit.Abstractions;

#pragma warning disable 1998

namespace ProxyKit
{
    public class RealStartup
    {
        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app)
        {
            var options = new ForwardedHeadersOptions();
            options.AllowedHosts.Add("*");
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            options.KnownProxies.Add(IPAddress.Parse("::ffff:127.0.0.1"));
            options.ForwardedHeaders = ForwardedHeaders.All;
            app.UseXForwardedHeaders(options);

            app.Map("/normal", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Ok");
            }));

            app.Map("/cachable", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Headers.Add("Cache-Control", "max-age=60");
                await ctx.Response.WriteAsync("Ok");
            }));
        }

        public static IWebHost BuildKestrelBasedServerOnRandomPort(ITestOutputHelper testOutputHelper)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseStartup<RealStartup>()
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new XunitLoggerProvider(testOutputHelper, "Upstream"));
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();
        }
    }
}