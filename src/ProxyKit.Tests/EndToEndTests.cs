using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class EndToEndTests
    {
        [Fact]
        public async Task Can_get_proxied_route()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<TestStartup>();

            using (var testServer = new TestServer(webHostBuilder))
            {
                var client = testServer.CreateClient();
                var result = await client.GetAsync("/accepted");
                result.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                result = await client.GetAsync("/forbidden");
                result.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            }
        }

        [Fact]
        public async Task Responses_from_real_server_are_handled_correctly()
        {
            using (var server = TestStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "4")
                    .UseStartup<TestStartup>()))
                {
                    var client = testServer.CreateClient();

                    // When server is running, response code should be 'ok'
                    var result = await client.GetAsync("/realServer/normal");
                    result.StatusCode.ShouldBe(HttpStatusCode.OK);

                    // error status codes should just be proxied
                    result = await client.GetAsync("/realServer/badrequest");
                    result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    result = await client.GetAsync("/realServer/error");
                    result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

                    // server timeouts should be returned as gateway timeouts
                    result = await client.GetAsync("/realServer/slow");
                    result.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);

                    // server timeouts should be 'delayed' 
                    using (var cts = new CancellationTokenSource())
                    {
                        cts.CancelAfter(TimeSpan.FromMilliseconds(1000));
                        result = await client.GetAsync("/realServer/slow", cts.Token);
                        result.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
                    }

                    // When server is stopped, should return 
                    await server.StopAsync();
                    result = await client.GetAsync("/realServer/normal");
                    result?.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
                }
            }
        }

        [Fact]
        public async Task When_upstream_host_is_not_running_then_should_get_service_unavailable()
        {
            using (var server = TestStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseStartup<TestStartup>()))
                {
                    var client = testServer.CreateClient();
                    // When server is running, response code should be 'ok'
                    var result = await client.GetAsync("/realServer/normal");
                    Assert.Equal(result.StatusCode, HttpStatusCode.OK);

                    // When server is stopped, should return ServiceUnavailable.
                    await server.StopAsync();
                    result = await client.GetAsync("/realServer/normal");
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                }
            }
        }

        [Fact]
        public async Task When_upstream_host_is_not_running_and_timeout_is_small_then_operation_cancelled_is_service_unavailable()
        {
            using (var server = TestStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "1")
                    .UseStartup<TestStartup>()))
                {
                    var client = testServer.CreateClient();
                    await server.StopAsync();
                    var result = await client.GetAsync("/realServer/normal");
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                }
            }
        }
    }

    internal static class WebHostExtensions
    {
        internal static int GetServerPort(this IWebHost server)
        {
            var address = server.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
            var match = Regex.Match(address, @"^.+:(\d+)$");
            var port = 0;

            if (match.Success)
            {
                port = int.Parse(match.Groups[1].Value);
            }

            return port;
        }
    }
}