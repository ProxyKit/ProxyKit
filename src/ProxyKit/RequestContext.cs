using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class RequestContext
    {
        private readonly HttpRequest _request;

        public RequestContext(HttpRequest request)
        {
            _request = request;
        }

        public string Method => _request.Method;

        public string Scheme => _request.Scheme;

        public bool IsHttps => _request.IsHttps;

        public HostString Host => _request.Host;

        public PathString PathBase => _request.PathBase;

        public PathString Path => _request.Path;

        public QueryString QueryString => _request.QueryString;

        public IQueryCollection Query => _request.Query;

        public string Protocol => _request.Protocol;

        public IHeaderDictionary Headers => _request.Headers;

        public IRequestCookieCollection Cookies => _request.Cookies;

        public string ContentType => _request.ContentType;
    }
}