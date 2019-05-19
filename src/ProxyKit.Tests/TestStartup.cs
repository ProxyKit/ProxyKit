using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable 1998

namespace ProxyKit
{
    public class TestStartup
    {
        private readonly IConfiguration _config;

        public TestStartup(IConfiguration config)
        {
            _config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var timeout = _config.GetValue("timeout", 60);
            services.AddProxy(httpClientBuilder =>
                httpClientBuilder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(timeout)));
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
    }
}