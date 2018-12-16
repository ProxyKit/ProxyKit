using System;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    /// <summary>
    ///     Represents an upstream host to which request can be forwarded to.
    /// </summary>
    public class UpstreamHost
    {
        public UpstreamHost(string scheme, HostString host, PathString pathBase = default)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(scheme));
            if (!host.HasValue)
                throw new ArgumentException("Value must be supplied", nameof(host));

            Scheme = scheme;
            Host = host;
            PathBase = pathBase;
        }

        public string Scheme { get; }

        public HostString Host { get; }

        public PathString PathBase { get; }

        public override string ToString()
        {
            return $"{Scheme}://{Host.ToString()}/{PathBase.Value}";
        }

        public static implicit operator UpstreamHost(string upstreamUri)
        {
            var uri = new Uri(upstreamUri);
            return new UpstreamHost(
                uri.Scheme,
                HostString.FromUriComponent(uri),
                PathString.FromUriComponent(uri));
        }

        public static implicit operator UpstreamHost(Uri upstreamUri) => new UpstreamHost(
            upstreamUri.Scheme,
            HostString.FromUriComponent(upstreamUri),
            PathString.FromUriComponent(upstreamUri));
    }
}
