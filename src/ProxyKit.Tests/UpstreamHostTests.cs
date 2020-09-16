using Microsoft.AspNetCore.Http;

using Xunit;

namespace ProxyKit
{
    public class UpstreamHostTests
    {
        [Fact]
        public void ConstructFromUri()
        {
            var host = new UpstreamHost("https://upstream.host");

            Assert.Equal("https://upstream.host/", host.Uri.AbsoluteUri);
            Assert.Equal("https", host.Scheme);
            Assert.Equal(new HostString("upstream.host", 443), host.Host);
            Assert.Equal(1u, host.Weight);
        }

        [Fact]
        public void ConstructFromUriWithNonDefaultPort()
        {
            var host = new UpstreamHost("HtTP://upstream.host:5000", 3);

            Assert.Equal("http://upstream.host:5000/", host.Uri.AbsoluteUri);
            Assert.Equal("http", host.Scheme);
            Assert.Equal(new HostString("upstream.host", 5000), host.Host);
            Assert.Equal(3u, host.Weight);
        }

        [Fact]
        public void ConstructFromParts()
        {
            var host = new UpstreamHost("WSS", new HostString("upstream.host"), weight: 2);

            Assert.Equal("wss://upstream.host/", host.Uri.AbsoluteUri);
            Assert.Equal("wss", host.Scheme);
            Assert.Equal(new HostString("upstream.host"), host.Host);
            Assert.Equal(2u, host.Weight);
        }

        [Fact]
        public void ConstructFromPartsWithNonDefaultPort()
        {
            var host = new UpstreamHost("ws", new HostString("upstream.host", 6000), weight: 2);

            Assert.Equal("ws://upstream.host:6000/", host.Uri.AbsoluteUri);
            Assert.Equal("ws", host.Scheme);
            Assert.Equal(new HostString("upstream.host", 6000), host.Host);
            Assert.Equal(2u, host.Weight);
        }

        [Fact]
        public void SchemeIsLowercase()
        {
            var host = new UpstreamHost("HTTP://upstream.host");

            Assert.Equal("http://upstream.host/", host.Uri.AbsoluteUri);
            Assert.Equal("http", host.Scheme);
            Assert.Equal(new HostString("upstream.host", 80), host.Host);
            Assert.Equal(1u, host.Weight);
        }
    }
}