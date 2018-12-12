using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace ProxyKit
{
    public static class PrepareRequestContextExtensions
    {
        public static void ApplyXForwardedHeaders(this PrepareRequestContext prepareRequest)
        {
            var headers = prepareRequest.DestinationRequestMessage.Headers;
            var protocol = prepareRequest.IncomingRequest.Scheme;
            var @for = prepareRequest.ConnectionInfo.RemoteIpAddress;
            var host = prepareRequest.IncomingRequest.Host;
            var pathBase = prepareRequest.IncomingRequest.PathBase.Value; // TODO should be escaped?

            headers.ApplyXForwardedHeaders(@for, host, protocol, pathBase);
        }

        public static void ApplyXForwardedHeaders(this ProxyContext context)
        {
            var headers = context.OutgoingRequestMessage.Headers;
            var protocol = context.IncomingRequest.Scheme;
            var @for = context.ConnectionInfo.RemoteIpAddress;
            var host = context.IncomingRequest.Headers["Host"];
            var hostString = HostString.FromUriComponent(host);
            var pathBase = context.IncomingRequest.PathBase.Value; // TODO should be escaped?

            headers.ApplyXForwardedHeaders(@for, hostString, protocol, pathBase);
        }
    }
}