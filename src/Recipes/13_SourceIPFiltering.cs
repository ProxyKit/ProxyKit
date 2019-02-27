using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class SourceIPFiltering : Recipe<ConditionalProxying.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                var ipNetwork = IPNetwork.Parse("10.0.0.1", "255.255.255.0");

                app.RunProxy(context =>
                {
                    // If the source IP is outside the specified range then return unauthorized.
                    // This uses the IPNetwork2 package from https://github.com/lduchosal/ipnetwork
                    if (!ipNetwork.Contains(context.Connection.RemoteIpAddress))
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                        return Task.FromResult(response);
                    }

                    return context
                        .ForwardTo("http://localhost:5001")
                        .AddXForwardedHeaders()
                        .Send();
                });
            }
        }
    }
}
