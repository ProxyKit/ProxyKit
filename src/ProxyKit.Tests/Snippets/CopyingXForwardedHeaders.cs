using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class CopyingXForwardedHeaders
{
    #region CopyingXForwardedHeaders
    public void Configure(IApplicationBuilder app)
    {
        app.RunProxy(context => context
            .ForwardTo("http://upstream-server:5001/")
            .CopyXForwardedHeaders()
            .Send());
    }
    #endregion
}