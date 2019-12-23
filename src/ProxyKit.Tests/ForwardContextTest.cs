using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using System.Collections;

namespace ProxyKit
{
    public class ForwardContextTest
    {
        private readonly Random _random;

        public ForwardContextTest()
        {
            _random = new Random();
        }

        private HttpContext CreateHttpContext(IServiceProvider provider)
        {
            var context = new DefaultHttpContext()
            {
                RequestServices = provider,
                RequestAborted = CancellationToken.None
            };
            context.Request.Method = HttpMethods.Get;
            return context;
        }

        [Fact]
        public void GetTimeoutTest()
        {
            var host = new UpstreamHost("http://localhost");
            var setupTimeout = TimeSpan.FromSeconds(_random.Next(1, 10));
            var services = new ServiceCollection()
                .AddProxy(builder =>
                {
                    builder.ConfigureHttpClient(http => http.Timeout = setupTimeout);
                }).BuildServiceProvider();

            var httpContext = CreateHttpContext(services);
            var forwardContext = httpContext.ForwardTo(host);
            Assert.Equal(setupTimeout, forwardContext.Timeout);
        }

        [Fact]
        public void SetTimeoutTest()
        {
            var host = new UpstreamHost("http://localhost");
            var runtimeTimeout = TimeSpan.FromSeconds(_random.Next(0, 10));
            HttpClient client = null;
            var services = new ServiceCollection()
                .AddProxy(builder =>
                {
                    builder.ConfigureHttpClient(http =>
                    {
                        client = http;
                    });
                }).BuildServiceProvider();
            var httpContext = CreateHttpContext(services);
            var forwardContext = httpContext.ForwardTo(host);
            forwardContext.Timeout = runtimeTimeout;
            Assert.NotNull(client);
            Assert.Equal(runtimeTimeout, client.Timeout);
        }

        [Fact]
        public void SetTimeout_ShouldNotEffectOthers()
        {
            var host = new UpstreamHost("http://localhost");
            var setupTimeout = TimeSpan.FromSeconds(_random.Next(1, 10));
            var services = new ServiceCollection()
                .AddProxy(builder =>
                {
                    builder.ConfigureHttpClient(http => http.Timeout = setupTimeout);
                }).BuildServiceProvider();

            var httpContext = CreateHttpContext(services);
            var forwardContext = httpContext.ForwardTo(host);
            forwardContext.Timeout = TimeSpan.FromMinutes(1);
            Assert.NotEqual(setupTimeout, forwardContext.Timeout);

            var otherForwardContext = httpContext.ForwardTo(host);            
            Assert.Equal(setupTimeout, otherForwardContext.Timeout);
        }
    }
}
