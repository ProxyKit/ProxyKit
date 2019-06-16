using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
                .UseStartup<ProxyStartup>();

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
            using (var server = RealStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "4")
                    .UseStartup<ProxyStartup>()))
                {
                    var client = testServer.CreateClient();
                    client.BaseAddress = new Uri("http://example.com:8080");

                    // When server is running, response code should be 'ok'
                    var result = await client.GetAsync("/realserver/normal");
                    result.StatusCode.ShouldBe(HttpStatusCode.OK);
                    var resultTypedHandler = await client.GetAsync("/realserver-typedhandler/normal");
                    resultTypedHandler.StatusCode.ShouldBe(HttpStatusCode.OK);

                    // error status codes should just be proxied
                    result = await client.GetAsync("/realserver/badrequest");
                    result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    result = await client.GetAsync("/realserver/error");
                    result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

                    // server timeouts should be returned as gateway timeouts
                    result = await client.GetAsync("/realserver/slow");
                    result.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);

                    // server timeouts should be 'delayed' 
                    using (var cts = new CancellationTokenSource())
                    {
                        cts.CancelAfter(TimeSpan.FromMilliseconds(1000));
                        result = await client.GetAsync("/realserver/slow", cts.Token);
                        result.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
                    }

                    // redirect location header should have correct host
                    result = await client.GetAsync("/realserver/redirect");
                    result.StatusCode.ShouldBe(HttpStatusCode.Redirect);
                    result.Headers.Location.ShouldBe(new Uri("http://example.com:8080/realserver/redirect"));

                    // When server is stopped, should return 
                    await server.StopAsync();
                    result = await client.GetAsync("/realserver/normal");
                    result?.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
                }
            }
        }

        [Fact]
        public async Task When_upstream_host_is_not_running_then_should_get_service_unavailable()
        {
            using (var server = RealStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseStartup<ProxyStartup>()))
                {
                    var client = testServer.CreateClient();
                    // When server is running, response code should be 'ok'
                    var result = await client.GetAsync("/realserver/normal");
                    Assert.Equal(result.StatusCode, HttpStatusCode.OK);

                    // When server is stopped, should return ServiceUnavailable.
                    await server.StopAsync();
                    result = await client.GetAsync("/realserver/normal");
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                }
            }
        }

        [Fact]
        public async Task When_upstream_host_is_not_running_and_timeout_is_small_then_operation_cancelled_is_service_unavailable()
        {
            using (var server = RealStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "1")
                    .UseStartup<ProxyStartup>()))
                {
                    var client = testServer.CreateClient();
                    await server.StopAsync();
                    var result = await client.GetAsync("/realserver/normal");
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                }
            }
        }

        [Fact]
        public async Task Can_proxy_websockets()
        {
            using (var server = RealStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "1")
                    .UseStartup<ProxyStartup>()))
                {
                    var client = testServer.CreateWebSocketClient();
                    var webSocket = await client.ConnectAsync(new Uri("ws://localhost/ws/"), CancellationToken.None);
                    await SendText(webSocket, "foo");
                    var result = await ReceiveText(webSocket);
                    result.ShouldBe("foo");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
                }
            }
        }

        [Fact]
        public async Task Can_proxy_websockets_with_request_customization()
        {
            using (var server = RealStartup.BuildKestrelBasedServerOnRandomPort())
            {
                await server.StartAsync();
                var port = server.GetServerPort();

                using (var testServer = new TestServer(new WebHostBuilder()
                    .UseSetting("port", port.ToString())
                    .UseSetting("timeout", "1")
                    .UseStartup<ProxyStartup>()))
                {
                    var client = testServer.CreateWebSocketClient();
                    var webSocket = await client.ConnectAsync(new Uri("ws://localhost/ws-custom/?a=b"), CancellationToken.None);
                    await SendText(webSocket, "foo");
                    var result = await ReceiveText(webSocket);
                    result.ShouldBe("X-TraceId=123?a=b"); // Custom websocket echos this header
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
                }
            }
        }

        private async Task SendText(WebSocket webSocket, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task<string> ReceiveText(WebSocket webSocket)
        {
            var buffer = new Memory<byte>(new byte[1024]);
            var receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            receiveResult.EndOfMessage.ShouldBeTrue();
            var echoResult = Encoding.UTF8.GetString(buffer.Slice(0, receiveResult.Count).Span);
            return echoResult;
        }
    }
}