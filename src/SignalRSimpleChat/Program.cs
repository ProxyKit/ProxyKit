using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ProxyKit.Recipe.SignalRSimpleChat
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var upstreamHost = WebHost.CreateDefaultBuilder(args)
                .UseStartup<SignalRChatStartup>()
                .UseUrls("http://localhost:5001")
                .Build();

            var proxyHost = WebHost.CreateDefaultBuilder(args)
                .UseStartup<ProxyStartup>()
                .UseUrls("http://localhost:5000")
                .Build();

            await upstreamHost.StartAsync();

            proxyHost.Run();
        }
    }
}
