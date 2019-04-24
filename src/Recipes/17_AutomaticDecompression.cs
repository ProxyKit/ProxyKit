using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class AutomaticDecompression
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                HttpMessageHandler CreatePrimaryHandler()
                {
                    var clientHandler = new HttpClientHandler
                    {
                        // Will automatically decompress content from upstream hosts
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };
                    return clientHandler;
                }

                services.AddProxy(httpClientBuilder =>
                    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler));
            }

            public void Configure(IApplicationBuilder app)
            {
                app.RunProxy(async context =>
                {
                    var response = await context
                        .ForwardTo("https://host/")
                        .AddXForwardedHeaders()
                        .Send();

                    // stream is decompressed
                    var body = await response.Content.ReadAsStringAsync();
                    var reverseBody = new string(body.Reverse().ToArray());
                    response.Content = new StringContent(
                        reverseBody,
                        Encoding.UTF8,
                        response.Content.Headers.ContentType.MediaType);
                    return response;
                });
            }
        }
    }
}