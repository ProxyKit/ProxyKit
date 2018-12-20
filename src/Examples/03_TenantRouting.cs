using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples
{
    public class MultiTenantJwt : ExampleBase<MultiTenantJwt.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
                services.AddAuthentication();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseAuthentication();
                app.RunProxy(
                    context =>
                    {
                        if (context.User == null)
                        {
                            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                        }
                        var tenantId = context.User.FindFirst("TenantId")?.Value;
                        if (tenantId == null)
                        {
                            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                        }

                        return context
                            .ForwardTo($"http://{tenantId}.internal:5001")
                            .Execute();
                    });
            }
        }
    }
}