using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProxyKit.DownstreamServer
{
    public class DownstreamStartup
    {

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvcCore()
                .UseSpecificControllers(typeof(Controller));
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            app.UseDeveloperExceptionPage();
            app.UseMvc();
        }
    }
}
