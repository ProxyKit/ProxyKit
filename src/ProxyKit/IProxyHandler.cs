using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    /// <summary>
    ///     Exposes a handler which supports forwarding a request to an upstream host.
    /// </summary>
    public interface IProxyHandler
    {
        /// <summary>
        ///     Represents a delegate that handles a proxy request.
        /// </summary>
        /// <param name="context">
        ///     An HttpContext that represents the incoming proxy request.
        /// </param>
        /// <returns>
        ///     A <see cref="HttpResponseMessage"/> that represents
        ///    the result of handling the proxy request.
        /// </returns>
        Task<HttpResponseMessage> HandleProxyRequest(HttpContext context);
    }
}