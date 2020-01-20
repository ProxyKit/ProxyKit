using System;
using ProxyKit;

#region CorrelationIdExtensions
public static class CorrelationIdExtensions
{
    public const string XCorrelationId = "X-Correlation-ID";

    public static ForwardContext ApplyCorrelationId(this ForwardContext forwardContext)
    {
        if (!forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
        {
            forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
        }
        return forwardContext;
    }
}
#endregion