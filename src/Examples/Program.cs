using System;
using System.Threading;
using EasyConsole;
using ProxyKit.Examples.Simple;

namespace ProxyKit.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, __) => cts.Cancel();
            new Menu()
                .Add(
                    "Simple Forwarding",
                    () => new SimpleExample().Run(cts.Token))
                .Display();
        }
    }
}
