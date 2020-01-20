using System;
using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class AddXCorrelationIDHeader
{
    #region AddXCorrelationIDHeader
    public const string XCorrelationId = "X-Correlation-ID";

    public void Configure(IApplicationBuilder app)
    {
        app.RunProxy(context =>
        {
            var forwardContext = context.ForwardTo("http://upstream-server:5001/");
            if (!forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
            {
                forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
            }
            return forwardContext.Send();
        });
    }
    #endregion
}