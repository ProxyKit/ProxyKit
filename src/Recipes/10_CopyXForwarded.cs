using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class CopyXForwarded : ExampleBase<CopyXForwarded.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy(context => context
                    .ForwardTo("http://localhost:5001")
                    .CopyXForwardedHeaders() // copies the headers from the incoming requests
                    .AddXForwardedHeaders() // adds the current proxy proto/host/for/pathbase to the X-Forwarded headers
                    .Send());
            }
        }
    }
}
