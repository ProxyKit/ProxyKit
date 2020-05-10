using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class KestrelServerAcceptanceTests: AcceptanceTestsBase
    {
        private IWebHost _upstreamServer;
        private IWebHost _proxyServer;

        public KestrelServerAcceptanceTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {}

        protected override int ProxyPort => _proxyServer.GetServerPort();

        protected override HttpClient CreateClient() =>
            new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{ProxyPort}")
            };

        [Fact]
        public async Task When_upstream_host_is_not_running_then_should_get_service_unavailable()
        {
            var client = CreateClient();
            await _upstreamServer.StopAsync();
            
            var response = await client.GetAsync("/normal");

            response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        }

        // This client test is only valid with a real network and server
        // https://github.com/dotnet/aspnetcore/issues/19541#issuecomment-594201070
        [Fact]
        public async Task Proxy_client_timeout_timeout_should_return_GatewayTimeout()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/slow");

            response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        }

        [Fact]
        public async Task Can_proxy_websockets()
        {
            var clientWebSocket = new ClientWebSocket();
            await clientWebSocket.ConnectAsync(new Uri($"ws://localhost:{ProxyPort}/ws/"), CancellationToken.None);
            await SendText(clientWebSocket, "foo");

            var result = await ReceiveText(clientWebSocket);
            
            result.ShouldBe("foo");

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
        }

        [Fact]
        public async Task Can_proxy_websockets_with_request_customization()
        {
            var clientWebSocket = new ClientWebSocket();
            await clientWebSocket.ConnectAsync(new Uri($"ws://localhost:{ProxyPort}/ws-custom/?a=b"), CancellationToken.None);
            await SendText(clientWebSocket, "foo");
            
            var result = await ReceiveText(clientWebSocket);

            result.ShouldBe("X-TraceId=123?a=b"); // Custom websocket echos this header

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
        }

        private async Task SendText(ClientWebSocket clientWebSocket, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            await clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task<string> ReceiveText(ClientWebSocket clientWebSocket)
        {
            var buffer = new Memory<byte>(new byte[1024]);
            var receiveResult = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
            receiveResult.EndOfMessage.ShouldBeTrue();
            var echoResult = Encoding.UTF8.GetString(buffer.Slice(0, receiveResult.Count).Span);
            return echoResult;
        }

        public override async Task InitializeAsync()
        {
            _upstreamServer = UpstreamBuilder
                .UseKestrel()
                .UseUrls("http://*:0")
                .Build();
            await _upstreamServer.StartAsync();
            
            var port = _upstreamServer.GetServerPort();

            _proxyServer = ProxyBuilder
                .UseKestrel()
                .UseUrls("http://*:0")
                .UseSetting("port", port.ToString())
                .ConfigureTestServices(services =>
                {
                    services
                        .AddProxy(httpClientBuilder => httpClientBuilder
                            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(1)));
                })
                .Build();
           
            await _proxyServer.StartAsync();
        }

        public override async Task DisposeAsync()
        {
            await _proxyServer.StopAsync();
            await _upstreamServer.StopAsync(TimeSpan.FromMilliseconds(100));
        }
    }
}
