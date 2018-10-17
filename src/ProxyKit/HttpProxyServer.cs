using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ReverseProxyToolkit
{
    public class HttpProxyServer
    {
        public static Task RunAsync(int port = 8080)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:{port}")
                .Configure(a => a.Run(c => c.Response.WriteAsync("Wat")))
                .Build()
                .RunAsync();
        }
    }
}
