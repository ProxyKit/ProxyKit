using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public static class XForwardedExtensions
    {
        public const string XForwardedFor = "X-Forwarded-For";
        public const string XForwardedHost = "X-Forwarded-Host";
        public const string XForwardedProto = "X-Forwarded-Proto";
        public const string XForwardedPathBase = "X-Forwarded-PathBase";

        /// <summary>
        ///     Applies X-Forwarded.* headers to the outgoing
        ///     header collection.
        /// </summary>
        /// <param name="outgoingHeaders">The outgoing HTTP request
        /// headers.</param>
        /// <param name="for">The client IP address.</param>
        /// <param name="host">The host of the request.</param>
        /// <param name="proto">The protocol of the incoming request.</param>
        public static void ApplyXForwardedHeaders(
            this HttpRequestHeaders outgoingHeaders,
            IPAddress @for,
            HostString host,
            string proto) 
            => ApplyXForwardedHeaders(outgoingHeaders, @for, host, proto, string.Empty);

        /// <summary>
        ///     Applies X-Forwarded.* headers to the outgoing header collection
        ///     with an additional PathBase parameter.
        /// </summary>
        /// <param name="outgoingHeaders">The outgoing HTTP request
        /// headers.</param>
        /// <param name="for">The client IP address.</param>
        /// <param name="host">The host of the request.</param>
        /// <param name="proto">The protocol of the incoming request.</param>
        /// <param name="pathBase">The base path of the incoming
        /// request.</param>
        public static void ApplyXForwardedHeaders(
            this HttpRequestHeaders outgoingHeaders,
            IPAddress @for,
            HostString host,
            string proto,
            PathString pathBase)
        {
            if (@for != null)
            {
                var forString = @for.AddressFamily == AddressFamily.InterNetworkV6
                    ? $"\"[{@for}]\""
                    : @for.ToString();
                outgoingHeaders.Add(XForwardedFor, forString);
            }

            if (host.HasValue)
            {
                outgoingHeaders.Add(XForwardedHost, host.Value);
            }

            if (!string.IsNullOrWhiteSpace(proto))
            {
                outgoingHeaders.Add(XForwardedProto, proto);
            }

            if (!string.IsNullOrWhiteSpace(pathBase))
            {
                outgoingHeaders.Add(XForwardedPathBase, pathBase);
            }
        }
    }
}