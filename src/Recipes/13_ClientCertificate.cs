using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class ClientCertificate : ExampleBase<ConditionalProxying.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                // get your client-certificate from: https://badssl.com/download/
                var certBytes = File.ReadAllBytes("./badssl.com-client.p12");
                var clientCertificate = new X509Certificate2(certBytes, "badssl.com");

                Func<HttpMessageHandler> createPrimaryHandler = () =>
                {
                    var _clientHandler = new HttpClientHandler();
                    _clientHandler.ClientCertificates.Add(clientCertificate);
                    _clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    return _clientHandler;
                };

                services.AddProxy(httpClientBuilder => httpClientBuilder.ConfigurePrimaryHttpMessageHandler(createPrimaryHandler));

            }

            public void Configure(IApplicationBuilder app)
            {

                app.RunProxy(context => context
                    .ForwardTo("https://client.badssl.com/")
                    .AddXForwardedHeaders()
                    .Send());

            }
        }
    }
}