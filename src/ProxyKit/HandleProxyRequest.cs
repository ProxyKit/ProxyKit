using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    /// <summary>
    ///     Represents a delegate that handles a proxy request.
    /// </summary>
    /// <param name="httpContext">
    ///     An HttpContext that represents the incoming proxy request.
    /// </param>
    /// <returns>
    ///     A <see cref="HttpResponseMessage"/> that represents
    ///    the result of handling the proxy request.
    /// </returns>
    public delegate Task<HttpResponseMessage> HandleProxyRequest(HttpContext httpContext);
}