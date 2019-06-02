using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipe.Simple
{
    public class ProxyStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.RunProxy(context => context
                .ForwardTo("http://localhost:5001")
                .AddXForwardedHeaders()
                .Send());
        }
    }
}