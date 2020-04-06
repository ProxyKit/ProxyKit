using System;

using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    /// <summary>
    ///     Represents an upstream host to which requests can be forwarded to.
    /// </summary>
    public sealed class UpstreamHost : IEquatable<UpstreamHost>
    {
        public UpstreamHost(
            string scheme,
            HostString host,
            PathString pathBase = default,
            uint weight = 1)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(scheme));
            if (!host.HasValue)
                throw new ArgumentException("Value must be supplied", nameof(host));

            Scheme = scheme.ToLowerInvariant();
            Host = host;
            PathBase = pathBase;
            Weight = weight;
            Uri = GetUri();
        }

        public UpstreamHost(
            string uri,
            uint weight = 1)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(uri));

            var upstreamUri = new Uri(uri);

            Scheme = upstreamUri.Scheme;
            Host = HostString.FromUriComponent(upstreamUri);
            PathBase = PathString.FromUriComponent(upstreamUri);
            Weight = weight;
            Uri = GetUri();
        }

        private Uri GetUri()
        {
            var port = Host.Port ?? GetDefaultPort(Scheme);
            var builder = new UriBuilder(Scheme, Host.Host, port, PathBase.Value);
            return builder.Uri;
        }

        private static int GetDefaultPort(string scheme)
        {
            return scheme switch
            {
                "http" => 80,
                "https" => 443,
                "ws" => 80,
                "wss" => 443,
                _ => throw new NotSupportedException()
            };
        }

        public string Scheme { get; }

        public HostString Host { get; }

        public PathString PathBase { get; }

        public uint Weight { get; }

        public Uri Uri { get; }

        public override string ToString()
        {
            return $"{Scheme}://{Host.ToString()}{PathBase.Value}";
        }

        public static implicit operator UpstreamHost(string uri) => new Uri(uri);

        public static implicit operator UpstreamHost(Uri upstreamUri) => new UpstreamHost(
            upstreamUri.Scheme,
            HostString.FromUriComponent(upstreamUri),
            PathString.FromUriComponent(upstreamUri));

        public bool Equals(UpstreamHost other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Scheme, other.Scheme, StringComparison.Ordinal)
                && Host.Equals(other.Host) 
                && PathBase.Equals(other.PathBase);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is UpstreamHost other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(Scheme, Host, PathBase);

        public static bool operator ==(UpstreamHost left, UpstreamHost right)
            => Equals(left, right);

        public static bool operator !=(UpstreamHost left, UpstreamHost right)
            => !Equals(left, right);
    }
}
