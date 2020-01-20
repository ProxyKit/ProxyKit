using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class AddXForwardedHeadersToRequestToUpstreamServer
{
    #region AddXForwardedHeadersToRequestToUpstreamServer
    public void Configure(IApplicationBuilder app)
    {
        app.RunProxy(context => context
            .ForwardTo("http://upstream-server:5001/")
            .AddXForwardedHeaders()
            .Send());
    }
    #endregion
}