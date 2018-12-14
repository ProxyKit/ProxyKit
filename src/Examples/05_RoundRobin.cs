using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples
{
    public class RoundRobinLoadBalancer : ExampleBase<RoundRobinLoadBalancer.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy(
                    options => options.ConfigureHttpClient = (_, client) 
                        => client.Timeout = TimeSpan.FromSeconds(5));
            }

            public void Configure(IApplicationBuilder app)
            {
                var hosts = new List<string>
                {
                    "http://localhost:5001",
                    "http://localhost:5002"
                };
                var roundRobin = new RoundRobin<string>(hosts);

                app.RunProxy(
                    (context, handle) =>
                    {
                        var host = roundRobin.Next();

                        var forwardContext = context
                            .ForwardTo(host)
                            .ApplyXForwardedHeaders();

                        return handle(forwardContext);
                    });
            }
        }
    }
}