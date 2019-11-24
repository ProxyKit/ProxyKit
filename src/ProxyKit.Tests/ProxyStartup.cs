using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable 1998

namespace ProxyKit
{
    public class ProxyStartup
    {
        private readonly IConfiguration _config;

        public ProxyStartup(IConfiguration config)
        {
            _config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var timeout = _config.GetValue("timeout", 60);
            services.AddProxy(httpClientBuilder => httpClientBuilder
                .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(timeout)));
            services.AddSingleton<TypedHandler>();
        }

        public void Configure(IApplicationBuilder app, IServiceProvider sp)
        {
            app.UseXForwardedHeaders();

            app.Map("/accepted", appInner => 
                appInner.RunProxy(async context 
                    => new HttpResponseMessage(HttpStatusCode.Accepted)));

            app.Map("/forbidden", appInner => 
                appInner.RunProxy(async context 
                    => new HttpResponseMessage(HttpStatusCode.Forbidden)));

            var port = _config.GetValue("Port", 0);
            if (port != 0)
            {
                app.Map("/realserver", appInner =>
                    appInner.RunProxy(context => context
                        .ForwardTo("http://localhost:" + port + "/")
                        .AddXForwardedHeaders()
                        .Send()));

                app.Map("/realserver-typedhandler", appInner =>
                    appInner.RunProxy<TypedHandler>());

                app.UseWebSockets();
                app.Map("/ws", appInner =>
                {
                    appInner.UseWebSocketProxy(
                        _ => new Uri($"ws://localhost:{port}/ws/"),
                        options => options.AddXForwardedHeaders());
                });

                app.Map("/ws-custom", appInner =>
                {
                    appInner.UseWebSocketProxy(
                        _ => new Uri($"ws://localhost:{port}/ws-custom/"),
                        options => options.SetRequestHeader("X-TraceId", "123"));
                });
            }
        }

        private class TypedHandler : IProxyHandler
        {
            private readonly IConfiguration _config;

            public TypedHandler(IConfiguration config)
            {
                _config = config;
            }

            public Task<HttpResponseMessage> HandleProxyRequest(HttpContext context)
            {
                var port = _config.GetValue("Port", 0);

                return context
                    .ForwardTo("http://localhost:" + port + "/")
                    .AddXForwardedHeaders()
                    .Send();
            }
        }
    }
}