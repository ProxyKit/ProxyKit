using Microsoft.AspNetCore.Builder;
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
               requestContext => requestContext.ForwardTo("http://localhost:5001"),
               prepareRequestContext => prepareRequestContext.ApplyXForwardedHeaders());
        }
    }
}
