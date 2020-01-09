using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class AddXCorrelationIDHeaderWithExtension
{
    #region AddXCorrelationIDHeaderWithExtension
    public void Configure(IApplicationBuilder app)
    {
        app.RunProxy(context => context
            .ForwardTo("http://upstream-server:5001/")
            .ApplyCorrelationId()
            .Send());
    }
    #endregion
}