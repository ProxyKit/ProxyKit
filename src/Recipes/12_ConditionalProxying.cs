using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class ConditionalProxying : Recipe<ConditionalProxying.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                // Forwards the request only when the host is set to the specified value
                app.UseWhen(
                    context => context.Request.Host.Host.Equals("api.example.com"),
                    appInner => appInner.RunProxy(context => context
                        .ForwardTo("http://localhost:5001")
                        .AddXForwardedHeaders()
                        .Send()));
            }
        }
    }
}
