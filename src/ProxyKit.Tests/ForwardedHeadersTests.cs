using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

        [Fact]
        public void Can_append_x_forwarded_headers()
        {
            _headers.ApplyXForwardedHeaders(IPAddress.Parse("1.2.3.4"), new HostString("example.com:8080"), "https");

            var forValue = _headers.GetValues(XForwardedExtensions.XForwardedFor).ToArray();
            var hostValue = _headers.GetValues(XForwardedExtensions.XForwardedHost).ToArray();
            var protoValue = _headers.GetValues(XForwardedExtensions.XForwardedProto).ToArray();

            forValue.ShouldBe(new [] { "1.2.3.4"} );
            hostValue.ShouldBe(new [] { "example.com:8080" });
            protoValue.ShouldBe(new [] { "https" });
        }

        [Fact]
        public void Can_append_x_forwarded_headers_multiple_times()
        {
            _headers.ApplyXForwardedHeaders(IPAddress.Parse("1.2.3.4"), new HostString("example.com"), "https");
            _headers.ApplyXForwardedHeaders(IPAddress.Parse("[2001:db8:cafe::17]"), new HostString("bar.com"), "http");

            var forValue = _headers.GetValues(XForwardedExtensions.XForwardedFor).ToArray();
            var hostValue = _headers.GetValues(XForwardedExtensions.XForwardedHost).ToArray();
            var protoValue = _headers.GetValues(XForwardedExtensions.XForwardedProto).ToArray();

            forValue.ShouldBe(new[] { "1.2.3.4", "\"[2001:db8:cafe::17]\"" });
            hostValue.ShouldBe(new[] { "example.com" , "bar.com" });
            protoValue.ShouldBe(new[] { "https" , "http" });
        }

        [Fact]
        public void Can_append_x_forwarded_headers_with_pathbase()
        {
            _headers.ApplyXForwardedHeaders(
                IPAddress.Parse("1.2.3.4"),
                new HostString("example.com"),
                "https",
                "/foo/");

            var pathBaseValue = _headers.GetValues(XForwardedExtensions.XForwardedPathBase).ToArray();

            pathBaseValue.ShouldBe(new[] { "/foo/" });
        }

        [Fact]
        public void Can_copy_x_forwarded_headers()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Headers.TryAdd(XForwardedExtensions.XForwardedFor, "127.0.0.1");
            httpContext.Request.Headers.TryAdd(XForwardedExtensions.XForwardedHost, "localhost");
            httpContext.Request.Headers.TryAdd(XForwardedExtensions.XForwardedProto, "http");
            httpContext.Request.Headers.TryAdd(XForwardedExtensions.XForwardedPathBase, "127.0.0.1");

            var services = new ServiceCollection();
            services.AddTransient<ProxyKitClient>();
            services.AddTransient(sp => new HttpClient());
            services.AddOptions<ProxyOptions>();
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            var forwardContext = httpContext
                .ForwardTo(new UpstreamHost("http://localhost"))
                .CopyXForwardedHeaders();

            forwardContext.UpstreamRequest.Headers.Contains(XForwardedExtensions.XForwardedFor).ShouldBeTrue();
            forwardContext.UpstreamRequest.Headers.Contains(XForwardedExtensions.XForwardedHost).ShouldBeTrue();
            forwardContext.UpstreamRequest.Headers.Contains(XForwardedExtensions.XForwardedProto).ShouldBeTrue();
            forwardContext.UpstreamRequest.Headers.Contains(XForwardedExtensions.XForwardedPathBase).ShouldBeTrue();
        }
    }
}