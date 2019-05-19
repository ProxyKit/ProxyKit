using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class Paths
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                // Forwards requests from /app1 to upstream host http://localhost:5001/foo/
                app.Map("/app1", app1 =>
                {
                    app1.RunProxy(context => context
                        .ForwardTo("http://localhost:5001/foo/")
                        .AddXForwardedHeaders()
                        .Send());
                });

                // Forwards requests from /app2 to upstream host http://localhost:5002/bar/
                app.Map("/app2", app2 =>
                {
                    app2.RunProxy(context => context
                        .ForwardTo("http://localhost:5002/bar/")
                        .AddXForwardedHeaders()
                        .Send());
                });
            }
        }
    }
}