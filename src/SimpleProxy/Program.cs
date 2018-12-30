using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyKit;

namespace SimpleProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            WebHost
                .CreateDefaultBuilder<Startup>(Array.Empty<string>())
                .UseUrls("http://+:5001")
                .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                .Build()
                .Run();
        }

        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy(context => context
                    .ForwardTo("http://10.0.75.1:5002")
                    .Execute());
            }
        }
    }
}
