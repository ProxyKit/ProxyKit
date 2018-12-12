using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace ProxyKit
{
    public class IncomingRequest
    {
        private readonly HttpRequest _request;

        public IncomingRequest(HttpRequest request)
        {
            _request = request;
        }

        /// <summary>
        ///     Gets the HTTP method.
        /// </summary>
        public string Method => _request.Method;

        /// <summary>
        ///     Gets the HTTP request scheme.
        /// </summary>
        public string Scheme => _request.Scheme;

        /// <summary>
        ///     Returns true if request scheme is HTTPS.
        /// </summary>
        public bool IsHttps => _request.IsHttps;

        /// <summary>
        ///     Gets the request path base.
        /// </summary>
        public PathString PathBase => _request.PathBase;

        /// <summary>
        ///     Gets the request path.
        /// </summary>
        public PathString Path => _request.Path;

        /// <summary>
        ///     Gets the request query string.
        /// </summary>
        public QueryString QueryString => _request.QueryString;

        /// <summary>
        ///     Gets the query value collection parsed from the query 
        /// </summary>
        public IQueryCollection Query => _request.Query;

        /// <summary>
        ///     Gets the request protocol
        /// </summary>
        public string Protocol => _request.Protocol;

        /// <summary>
        ///     Gets the collection of headers.
        /// </summary>
        public IHeaderDictionary Headers => _request.Headers;

        public RequestHeaders GetTypeHeaders() => _request.GetTypedHeaders();
    }
}