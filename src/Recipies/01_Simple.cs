using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipies
{
    public class Simple : ExampleBase<Simple.Startup>
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
                    .ApplyXForwardedHeaders()
                    .Execute());
            }
        }
    }
}
