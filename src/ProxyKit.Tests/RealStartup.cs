using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit
{
    public class RealStartup
    {
        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app)
        {
            app.Map("/normal", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Ok");
            }));

            app.Map("/badrequest", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync("Nah..");
            }));

            app.Map("/slow", a => a.Run(async ctx =>
            {
                await Task.Delay(5000);
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Ok... i guess");
            }));

            app.Map("/error", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync("cute..... BUT IT'S WRONG!");
            }));
        }
    }
}