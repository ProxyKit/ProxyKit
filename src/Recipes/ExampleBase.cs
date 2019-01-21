using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ProxyKit.Recipes
{
    public abstract class ExampleBase<T> where T : class
    {
        public Task Run(CancellationToken cancellationToken)
        {
            return WebHost.CreateDefaultBuilder<T>(Array.Empty<string>())
                .UseUrls("http://localhost:5000")
                .Build()
                .RunAsync(cancellationToken);
        }
    }
}