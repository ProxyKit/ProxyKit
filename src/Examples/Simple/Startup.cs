using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.Simple
{
    public class SimpleExample : ExampleBase
    {
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await WebHost.CreateDefaultBuilder<Startup>(Array.Empty<string>())
                .Build()
                .RunAsync(cancellationToken);
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.RunProxy((context, handle) =>
            {
                context.ForwardTo("http://localhost:5001");
                context.ApplyXForwardedHeaders();
                return handle();
            });
        }
    }
}
