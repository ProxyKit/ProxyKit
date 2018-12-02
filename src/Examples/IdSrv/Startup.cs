using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.IdSrv
{
    public class Startup
    {
        private readonly AppConfiguration appConfiguration;

        public Startup(IConfiguration configuration)
        {
            this.appConfiguration = new AppConfiguration();
            configuration.Bind(this.appConfiguration);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect("IdSrv", options =>
                {
                    options.SignInScheme = "Cookies";

                    options.Authority = this.appConfiguration.Authority;

                    options.ClientId = this.appConfiguration.ClientId;
                    options.ClientSecret = this.appConfiguration.ClientSecret;
                    options.ResponseType = "code id_token";

                    options.Scope.Add("api");
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("offline_access");
                });

            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync("IdSrv", new AuthenticationProperties() {RedirectUri = context.Request.GetEncodedUrl()});
                    return;
                }

                await next();
            });

            app.RunProxy(
                requestContext => requestContext.ForwardTo(this.appConfiguration.ForwardUrl),
                prepareRequestContext => prepareRequestContext.ApplyXForwardedHeader());
        }
    }
}