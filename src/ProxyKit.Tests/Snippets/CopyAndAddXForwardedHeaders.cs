using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class CopyAndAddXForwardedHeaders
{
    #region CopyAndAddXForwardedHeaders
    public void Configure(IApplicationBuilder app)
    {
        app.RunProxy(context => context
            .ForwardTo("http://upstream-server:5001/")
            .CopyXForwardedHeaders()
            .AddXForwardedHeaders()
            .Send());
    }
    #endregion
}