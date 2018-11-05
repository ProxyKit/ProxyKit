using Microsoft.AspNetCore.Http.Internal;

namespace ProxyKit
{
    public static class PrepareRequestContextExtensions
    {
        public static void ApplyForwardedHeader(this PrepareRequestContext prepareRequest)
        {
            var headers = prepareRequest.DestinationRequestMessage.Headers;
            var protocol = prepareRequest.IncomingRequest.Protocol;
            var @for = prepareRequest.ConnectionInfo.RemoteIpAddress;
            var host = prepareRequest.IncomingRequest.Host;
            var pathBase = prepareRequest.IncomingRequest.PathBase.Value; // TODO should be escaped?

            headers.ApplyForwardedHeaders(@for, host, protocol, pathBase);
        }
    }
}