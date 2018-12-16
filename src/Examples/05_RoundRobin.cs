using System;
using System.Collections.Generic;
using System.Net;
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
                var hosts = new List<UpstreamHost>
                {
                    "http://localhost:5001",
                    "http://localhost:5002"
                };
                var roundRobin = new RoundRobin(hosts);

                app.RunProxy(
                    async context =>
                    {
                        var host = roundRobin.Next();

                        var response = await context
                            .ForwardTo(host)
                            .ApplyXForwardedHeaders()
                            .Handle();

                        // failover
                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            return await context
                                .ForwardTo(host)
                                .ApplyXForwardedHeaders()
                                .Handle();
                        }

                        return response;
                    });
            }
        }
    }
}