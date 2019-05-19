using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit.XForwardedMiddleware
{
    public class XForwardedHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public XForwardedHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            var originalPathBase = context.Request.PathBase;
            if (context.Request.Headers.TryGetValue(XForwardedExtensions.XForwardedPathBase, out var pathBase))
            {
                context.Request.PathBase = new PathString(pathBase);
                context.Request.Headers.Add(
                    "X-Original-PathBase",
                    originalPathBase.HasValue ? originalPathBase.Value : "/");
            }
            return _next(context);
        }
    }
}
