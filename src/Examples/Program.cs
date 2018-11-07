using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ProxyKit.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder<IdSrv.Startup>(args)
                .Build()
                .Run();
        }
    }
}
