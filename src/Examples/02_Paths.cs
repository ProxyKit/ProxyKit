using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples
{
    public class Paths : ExampleBase<Paths.Startup>
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
                    "/app1",
                    (context, handle) =>
                    {
                        var forwardContext = context
                            .ForwardTo("http://localhost:5001/foo/")
                            .ApplyXForwardedHeaders();

                        return handle(forwardContext);
                    });

                app.RunProxy("/app2",
                    (context, handle) =>
                    {
                        var fowardContext = context
                            .ForwardTo("http://localhost:5002/bar/")
                            .ApplyXForwardedHeaders();

                        return handle(fowardContext);
                    });
            }
        }
    }
}