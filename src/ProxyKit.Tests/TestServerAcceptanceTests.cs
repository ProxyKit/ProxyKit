using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using ProxyKit.RoutingHandler;
using Xunit.Abstractions;

namespace ProxyKit
{
    public class TestServerAcceptanceTests: AcceptanceTestsBase
    {
        private TestServer _upstreamTestServer;
        private TestServer _proxyTestServer;
        private int _proxyPort;

        public TestServerAcceptanceTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {}

        protected override int ProxyPort => _proxyPort;

        protected override HttpClient CreateClient(bool useCookies = true)
        {
            var handler = useCookies 
                ? new CookieHandler(_proxyTestServer.CreateHandler(), CookieContainer)
                : _proxyTestServer.CreateHandler();

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri($"http://localhost:{_proxyPort}/")
            };
            return client;
        }

        public override Task InitializeAsync()
        {
            var router = new RoutingMessageHandler();
            _upstreamTestServer = new TestServer(UpstreamBuilder);
            _proxyPort = 81;

            var proxyBuilder = ProxyBuilder
                .UseSetting("port", _proxyPort.ToString())
                .ConfigureTestServices(services =>
                {
                    services.AddProxy(c =>
                    {
                        c.ConfigurePrimaryHttpMessageHandler(() => router);
                        c.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(1));
                    });
                });

            _proxyTestServer = new TestServer(proxyBuilder);

            router.AddHandler(new Origin("localhost", _proxyPort), _upstreamTestServer.CreateHandler());

            return Task.CompletedTask;
        }

        public override Task DisposeAsync()
        {
            _upstreamTestServer.Dispose();
            _proxyTestServer.Dispose();
            return Task.CompletedTask;
        }

        public class CookieHandler : DelegatingHandler
        {
            private readonly CookieContainer _cookieContainer;

            public CookieHandler(HttpMessageHandler innerHandler, CookieContainer cookieContainer)
                : base(innerHandler)
            {
                _cookieContainer = cookieContainer;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                var requestUri = request.RequestUri;
                request.Headers.Add(HeaderNames.Cookie, _cookieContainer.GetCookieHeader(requestUri));

                var response = await base.SendAsync(request, ct);

                if (response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieHeaders))
                {
                    foreach (var cookieHeader in SetCookieHeaderValue.ParseList(setCookieHeaders.ToList()))
                    {
                        Cookie cookie = new Cookie(cookieHeader.Name.Value, cookieHeader.Value.Value, cookieHeader.Path.Value);
                        if (cookieHeader.Expires.HasValue)
                        {
                            cookie.Expires = cookieHeader.Expires.Value.DateTime;
                        }
                        _cookieContainer.Add(requestUri, cookie);
                    }
                }

                return response;
            }
        }
    }
}