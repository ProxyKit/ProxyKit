using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipe.SignalRSimpleChat
{
    public class SignalRChatStartup
    {
        public SignalRChatStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app)
        {
            //app.UsePathBase("/subpath");

            app.Map("/subpath", appInner =>
            {
                appInner.UseStaticFiles();

                appInner.UseSignalR(routes =>
                {
                    routes.MapHub<Chat>("/chat");
                });

                appInner.UseMvcWithDefaultRoute();
            });
        }
    }
}
