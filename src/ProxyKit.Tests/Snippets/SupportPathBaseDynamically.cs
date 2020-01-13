using System.Linq;
using Microsoft.AspNetCore.Builder;

public class SupportPathBaseDynamically
{
    #region SupportPathBaseDynamically
    public void Configure(IApplicationBuilder app)
    {
        var options = new ForwardedHeadersOptions
        {
            //Other options
        };
        app.UseForwardedHeaders(options);
        app.Use((context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
            {
                context.Request.PathBase = pathBases.First();
            }
            return next();
        });
    }
    #endregion
}