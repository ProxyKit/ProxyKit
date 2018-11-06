using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Examples.MultitenantJWT
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
        }

        public void Configure(IApplicationBuilder app)
        {
           app.RunProxy(
               requestContext =>
               {
                   // Example of how to route a request to a backend host based on TenantId claim
                   // in a JWT Bearer token. Note: the backend service should still token validation.
                   // (Token validation can also be done here).
                   var authorization = requestContext.Headers.Get<string>("Authorization");
                   if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer"))
                   {
                       return HttpStatusCode.Unauthorized;
                   }
                   var token = authorization.Substring(0, "Bearer ".Length);
                   var handler = new JwtSecurityTokenHandler();
                   var jwtToken = handler.ReadJwtToken(token);
                   var tenantIdClaim = jwtToken.Claims.SingleOrDefault(c => c.Type == "TenantId");

                   if (tenantIdClaim == null)
                   {
                       return HttpStatusCode.Unauthorized;
                   }

                   return requestContext.ForwardTo($"http://{tenantIdClaim.Value}.internal:5001");
               },
               prepareRequestContext => prepareRequestContext.ApplyForwardedHeader());
        }
    }
}
