using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ProxyKit.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder<Simple.Startup>(args)
                .Build()
                .Run();
        }
    }
}
