using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.MultitenantJWT
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
               {
                   var authorization = requestContext.Headers.Get<string>("Authorization");
                   if (string.IsNullOrWhiteSpace(authorization))
                   {
                   }
                   return requestContext.ForwardTo("http://localhost:5001");
               },
               prepareRequestContext => prepareRequestContext.ApplyForwardedHeader());
        }
    }
}
