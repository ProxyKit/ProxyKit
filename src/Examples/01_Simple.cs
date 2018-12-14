using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples
{
    public class Basic : ExampleBase<Basic.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy((context, handle) =>
                {
                    var forwardContext = context
                        .ForwardTo("http://localhost:5001")
                        .ApplyXForwardedHeaders();

                    return handle(forwardContext);
                });
            }
        }
    }
}
