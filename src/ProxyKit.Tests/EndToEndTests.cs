using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class EndToEndTests : IDisposable
    {
        private TestServer _testServer;

        public EndToEndTests()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<TestStartup>();
            _testServer = new TestServer(webHostBuilder);
        }

        [Fact]
        public async Task Can_get_proxied_route()
        {
            var client = _testServer.CreateClient();
            var result = await client.GetAsync("/accepted");
            result.StatusCode.ShouldBe(HttpStatusCode.Accepted);

            result = await client.GetAsync("/forbidden");
            result.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        public void Dispose()
        {
            _testServer?.Dispose();
        }
    }

    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy(); //Required to add this.
        }

        public void Configure(IApplicationBuilder app)
        {
            // Can return the destination URI in three different ways.
            app.RunProxy(
                "/accepted",
                requestContext => HttpStatusCode.Accepted);

            app.RunProxy("/forbidden", requestContext => HttpStatusCode.Forbidden);

        }
    }
}
