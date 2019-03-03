using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipes
{
    public class ClientCertificate : ExampleBase<ClientCertificate.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                // get your client-certificate from: https://badssl.com/download/
                var certBytes = File.ReadAllBytes("./badssl.com-client.p12");
                var clientCertificate = new X509Certificate2(certBytes, "badssl.com");

                HttpMessageHandler CreatePrimaryHandler()
                {
                    var clientHandler = new HttpClientHandler();
                    clientHandler.ClientCertificates.Add(clientCertificate);
                    clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    return clientHandler;
                }

                services.AddProxy(httpClientBuilder => httpClientBuilder.ConfigurePrimaryHttpMessageHandler((Func<HttpMessageHandler>) CreatePrimaryHandler));
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