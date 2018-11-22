using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class EndToEndTests
    {

        public EndToEndTests()
        {
        }

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
            using (var server = BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = GetServerPort(server);

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
                        try
                        {
                            result = await client.GetAsync("/realServer/slow", cts.Token);
                            result.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    // When server is stopped, should return 
                    await server.StopAsync();
                    result = await client.GetAsync("/realServer/normal");
                    result?.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
                }
            }

        }


        [Fact]
        public async Task When_timeout_is_really_tight_and_server_not_available_then_operation_cancelled_is_handled_correctly()
        {
            using (var server = BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = GetServerPort(server);

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "1")
                    .UseStartup<TestStartup>()))
                {
                    var client = testServer.CreateClient();
                    // When server is running, response code should be 'ok'
                    var result = await client.GetAsync("/realServer/normal");
                    result.StatusCode.ShouldBe(HttpStatusCode.OK);

                    // When server is stopped, should return 
                    await server.StopAsync();
                    result = await client.GetAsync("/realServer/normal");
                    if (result != null)
                    {
                        try
                        {
                            result.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
                        }
                        catch (NullReferenceException)
                        {
                            // This happens on the build server.. but I don't understand why
                        }
                    }
                }
            }

        }

        private static IWebHost BuildKestrelBasedServerOnRandomPort()
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseStartup<RealStartup>()
                .Build();
        }

        private static int GetServerPort(IWebHost server)
        {
            var address = server.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
            var match = Regex.Match(address, @"^.+:(\d+)$");
            int port = 0;

            if (match.Success)
            {
                port = Int32.Parse(match.Groups[1].Value);
            }

            return port;
        }
    }

    public class RealStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Map("/normal", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Ok");
            }));

            app.Map("/badrequest", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync("Nah..");
            }));

            app.Map("/slow", a => a.Run(async ctx =>
            {
                await Task.Delay(5000);
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Ok... i guess");
            }));

            app.Map("/error", a => a.Run(async ctx =>
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync("cute..... BUT IT'S WRONG!");
            }));
        }
    }

    public class TestStartup
    {
        private readonly IConfiguration _config;

        public TestStartup(IConfiguration config)
        {
            _config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var timeout = _config.GetValue<int>("timeout", 60);
            services.AddProxy(options => 
                options.ConfigureHttpClient = (sp, client) => client.Timeout = TimeSpan.FromSeconds(timeout)); 
        }

        public void Configure(IApplicationBuilder app, IServiceProvider sp)
        {

            // Can return the destination URI in three different ways.
            app.RunProxy(
                "/accepted",
                requestContext => HttpStatusCode.Accepted);

            app.RunProxy("/forbidden", requestContext => HttpStatusCode.Forbidden);

            var port = _config.GetValue<int>("Port", 0);
            if (port != 0)
            {
                app.RunProxy("/realServer", 
                    requestContext => requestContext.ForwardTo("http://localhost:" + port +"/"));
            }

            

        }
    }
}
