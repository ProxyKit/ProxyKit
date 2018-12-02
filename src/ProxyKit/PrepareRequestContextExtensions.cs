using Microsoft.AspNetCore.Http.Internal;

namespace ProxyKit
{
    public static class PrepareRequestContextExtensions
    {
        public static void ApplyXForwardedHeader(this PrepareRequestContext prepareRequest)
        {
            var headers = prepareRequest.DestinationRequestMessage.Headers;
            var protocol = prepareRequest.IncomingRequest.Protocol;
            var @for = prepareRequest.ConnectionInfo.RemoteIpAddress;
            var host = prepareRequest.IncomingRequest.Host;
            var pathBase = prepareRequest.IncomingRequest.PathBase.Value; // TODO should be escaped?

            headers.ApplyXForwardedHeaders(@for, host, protocol, pathBase);
        }
    }
}