using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ProxyKit;

#region TypedHandler

public class MyTypedHandler : IProxyHandler
{
    private IUpstreamHostLookup _upstreamHostLookup;

    public MyTypedHandler(IUpstreamHostLookup upstreamHostLookup)
    {
        _upstreamHostLookup = upstreamHostLookup;
    }

    public Task<HttpResponseMessage> HandleProxyRequest(HttpContext context)
    {
        var upstreamHost = _upstreamHostLookup.Find(context);
        return context
            .ForwardTo(upstreamHost)
            .AddXForwardedHeaders()
            .Send();
    }
}
#endregion

public interface IUpstreamHostLookup
{
    UpstreamHost Find(HttpContext context);
}