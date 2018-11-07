using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class ForwardedHeadersTests
    {
        private readonly HttpRequestHeaders _headers;

        public ForwardedHeadersTests()
        {
            var httpRequestMessage = new HttpRequestMessage();
            _headers = httpRequestMessage.Headers;
        }


        [Theory]
        [InlineData(null, null, null, null, null)]
        [InlineData(null, null, null, "/foo/bar/", "pathbase=/foo/bar/")]
        [InlineData(null, null, "example.com", null, "host=example.com")]
        [InlineData(null, "1.2.3.4", null, null, "for=1.2.3.4")]
        [InlineData(null, "2001:db8:cafe::17", null, null, "for=\"[2001:db8:cafe::17]\"")]
        [InlineData("http", null, null, null, "proto=http")]
        [InlineData("http", "1.2.3.4", "example.com", "/foo/bar/", "for=1.2.3.4; host=example.com; proto=http; pathbase=/foo/bar/")]
        public void Can_apply_forwarded_headers(
            string proto,
            string @for,
            string host,
            string pathBase,
            string expected)
        {
            var ipAddress = @for == null ? null : IPAddress.Parse(@for);
            _headers.ApplyForwardedHeaders(ipAddress, new HostString(host), proto, pathBase);

            if (expected == null)
            {
                _headers.Contains(ForwardedExtensions.Forwarded).ShouldBeFalse();
            }
            else
            {
                _headers.Contains(ForwardedExtensions.Forwarded).ShouldBeTrue();
                _headers.GetValues(ForwardedExtensions.Forwarded)
                    .SingleOrDefault()
                    .ShouldBe(expected);
            }
        }

        [Fact]
        public void Can_append_forwarded_headers()
        {
            _headers.ApplyForwardedHeaders(IPAddress.Any, new HostString("example.com"), "https", "/");

            _headers.ApplyForwardedHeaders(IPAddress.Loopback, new HostString("localhost", 4043), "http", "/foo/");

            var forwardedHeaders = _headers.GetValues(ForwardedExtensions.Forwarded).ToArray();

            forwardedHeaders.Length.ShouldBe(2);
            forwardedHeaders.Last().ShouldContain("localhost");
        }
    }
}