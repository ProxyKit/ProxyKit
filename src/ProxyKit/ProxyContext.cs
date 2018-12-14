using System;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ProxyContext
    {
        public ProxyContext(ConnectionInfo connection,
            HttpRequest request,
            CancellationToken requestAborted,
            IServiceProvider requestServices)
        {
            Connection = connection;
            IncomingRequest = request;
            RequestAborted = requestAborted;
            RequestServices = requestServices;
        }

        public ConnectionInfo Connection { get; }

        public HttpRequest IncomingRequest { get; }

        public CancellationToken RequestAborted { get; }

        public IServiceProvider RequestServices { get; }
    }
}