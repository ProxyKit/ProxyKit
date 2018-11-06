using System;
using System.Net;

namespace ProxyKit
{
    public class ProxyResult
    {
        public Uri DestinationUri { get; }

        public HttpStatusCode? StatusCode { get; }

        public ProxyResult(HttpStatusCode statusCode) 
            => StatusCode = statusCode;

        public ProxyResult(Uri destinationUri)
            => DestinationUri = destinationUri;

        public static implicit operator ProxyResult(Uri destinationUri) 
            => new ProxyResult(destinationUri);

        public static implicit operator ProxyResult(HttpStatusCode statusCode)
            => new ProxyResult(statusCode);
    }
}