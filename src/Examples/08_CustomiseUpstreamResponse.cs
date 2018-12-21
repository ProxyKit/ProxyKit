using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples
{
    public class CustomiseUpstreamResponse : ExampleBase<Basic.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy(async context =>
                {
                    var response = await context
                        .ForwardTo("http://localhost:5001")
                        .ApplyXForwardedHeaders()
                        .Execute();

                    response.Headers.Remove("MachineID");

                    return response;
                });
            }
        }
    }
}
