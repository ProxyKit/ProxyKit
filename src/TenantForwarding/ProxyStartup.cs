using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipe.Simple
{
    public class ProxyStartup
    {
        private static readonly IReadOnlyDictionary<string, string> TenantHostsByTenantId = new Dictionary<string, string>
        {
            {"1", "http://localhost:5001"},
            {"2", "http://localhost:5002"}
        };
        private const string TenantIdClaimType = "tenantId";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                .AddBasic(
                    options =>
                    {
                        // For the sake of simplicity, we are using basic auth from https://github.com/blowdart/idunno.Authentication
                        // When a user is authenticated, they will have a claim "tenantId" added to their identity.
                        // If username is "user1" then the request will be forwarded to tenant 1's host. 
                        // If username is "user2" then the request will be forwarded to tenant 2's host.
                        options.Realm = "idunno";
                        options.AllowInsecureProtocol = true; //don't do this in production!
                        options.Events = new BasicAuthenticationEvents
                        {
                            OnValidateCredentials = context =>
                            {
                                if(context.Username == "user1")
                                {
                                    var claims = new[]
                                    {
                                        new Claim(
                                            TenantIdClaimType,
                                            "1",
                                            ClaimValueTypes.String,
                                            context.Options.ClaimsIssuer),
                                    };

                                    context.Principal = new ClaimsPrincipal(
                                        new ClaimsIdentity(claims, context.Scheme.Name));

                                    context.Success();
                                }
                                else if (context.Username == "user2")
                                {
                                    var claims = new[]
                                    {
                                        new Claim(
                                            TenantIdClaimType,
                                            "2",
                                            ClaimValueTypes.String,
                                            context.Options.ClaimsIssuer),
                                    };

                                    context.Principal = new ClaimsPrincipal(
                                        new ClaimsIdentity(claims, context.Scheme.Name));

                                    context.Success();
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.Use(async (context, next) =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    await context.AuthenticateAsync();
                }
                await next();
            });
            app.RunProxy(
                context =>
                {
                    if (!context.User.Identity.IsAuthenticated)
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                    }
                    var tenantId = context.User.FindFirst(TenantIdClaimType)?.Value;
                    if (tenantId != null)
                    {
                        // We defined the tenantId to host map statically. This of course could
                        // be looked up dynamically.
                        if (TenantHostsByTenantId.TryGetValue(tenantId, out var tenantHost))
                        {
                            return context
                                .ForwardTo(tenantHost)
                                .Send();
                        }
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));

                    }
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                });
        }
    }
}