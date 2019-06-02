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
            var tenant1Host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Tenant1Startup>()
                .UseUrls("http://localhost:5001")
                .Build();

            var tenant2Host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Tenant2Startup>()
                .UseUrls("http://localhost:5002")
                .Build();

            var proxyHost = WebHost.CreateDefaultBuilder(args)
                .UseStartup<ProxyStartup>()
                .Build();

            await tenant1Host.StartAsync();
            await tenant2Host.StartAsync();

            proxyHost.Run();
        }
    }
}