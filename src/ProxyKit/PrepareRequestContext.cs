using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class PrepareRequestContext
    {
        public PrepareRequestContext(
            HttpRequest httpRequest,
            ConnectionInfo connectionInfo,
            HttpRequestMessage destinationRequestMessage)
        {
            IncomingRequest = httpRequest;
            DestinationRequestMessage = destinationRequestMessage;
            ConnectionInfo = connectionInfo;
        }

        public HttpRequestMessage DestinationRequestMessage { get; }

        public HttpRequest IncomingRequest { get; }

        public ConnectionInfo ConnectionInfo { get; }
    }
}