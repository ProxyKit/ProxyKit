using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ProxyKit.Recipe.Simple
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var upstreamHost1 = WebHost.CreateDefaultBuilder(args)
                .UseStartup<UpstreamHost1Startup>()
                .UseUrls("http://localhost:5001")
                .Build();

            var upstreamHost2 = WebHost.CreateDefaultBuilder(args)
                .UseStartup<UpstreamHost1Startup>()
                .UseUrls("http://localhost:5002")
                .Build();

            var proxyHost = WebHost.CreateDefaultBuilder(args)
                .UseStartup<ProxyStartup>()
                .Build();

            await upstreamHost1.StartAsync();
            await upstreamHost2.StartAsync();

            proxyHost.Run();
        }
    }
}