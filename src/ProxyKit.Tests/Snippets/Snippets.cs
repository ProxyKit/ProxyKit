using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using ProxyKit;

public class Snippets
{
    public void Configure(IServiceCollection services)
    {
        #region ConfigureHttpTimeout
        services.AddProxy(httpClientBuilder =>
            httpClientBuilder.ConfigureHttpClient(client =>
                client.Timeout = TimeSpan.FromSeconds(5)));
        #endregion

        HttpMessageHandler _testMessageHandler = null;
        #region ConfigurePrimaryHttpMessageHandler
        services.AddProxy(httpClientBuilder =>
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(
                () => _testMessageHandler));
        #endregion
    }
}