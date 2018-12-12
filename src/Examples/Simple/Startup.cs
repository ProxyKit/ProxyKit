using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.Simple
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
           app.RunProxy(
               requestContext => 
                   requestContext.ForwardTo("http://localhost:5001"),
               prepareRequestContext => 
                   prepareRequestContext.ApplyXForwardedHeaders());

            app.RunProxy2(
                (context, forwardToHost) =>
                {
                    context.ApplyXForwardedHeaders();
                    return forwardToHost(context, "http://localhost:5001");
                });
        }
    }
}
