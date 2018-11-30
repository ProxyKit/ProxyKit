using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public static class ForwardedExtensions
    {
        public const string Forwarded = "Forwarded";

        /// <summary>
        ///     Applies Forwarded headers to the outgoing header collection.
        /// </summary>
        /// <remarks>
        ///     Not a pure function because <see cref="HttpRequestHeaders"/>
        ///     ctor is internal.
        /// </remarks>
        /// <param name="outgoingHeaders">The outgoing HTTP request headers.</param>
        /// <param name="for">The client IP address.</param>
        /// <param name="host">The host of the request.</param>
        /// <param name="proto">The protocol of the incoming request.</param>
        /// <param name="pathBase">The base path of the incoming request.</param>
        public static void ApplyForwardedHeaders(
            this HttpRequestHeaders outgoingHeaders,
            IPAddress @for,
            HostString host,
            string proto,
            PathString pathBase)
        {
            var forwardedHeader = new ForwardedHeader(@for, host, proto, pathBase);
            var headerValue = forwardedHeader.ToString();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                outgoingHeaders.Add(Forwarded, forwardedHeader.ToString());
            }
        }

        private class ForwardedHeader
        {
            private readonly string _proto;
            private readonly string _pathBase;
            private readonly IPAddress _for;
            private readonly HostString _host;

            public ForwardedHeader(IPAddress @for, HostString host, string proto, string pathBase)
            {
                _proto = proto;
                _pathBase = pathBase;
                _for = @for;
                _host = host;
            }

            public override string ToString()
            {
                var entries = new List<string>();
                if (_for != null)
                {
                    entries.Add(_for.AddressFamily == AddressFamily.InterNetworkV6 
                        ? $"for=\"[{_for}]\"" 
                        : $"for={_for}");
                }

                if (_host.HasValue)
                {
                    entries.Add($"host={_host.Host}");
                }

                if (!string.IsNullOrWhiteSpace(_proto))
                {
                    entries.Add($"proto={_proto}");
                }

                if (!string.IsNullOrWhiteSpace(_pathBase))
                {
                    entries.Add($"pathbase={_pathBase}");
                }

                return string.Join("; ", entries);
            }
        }
    }
}