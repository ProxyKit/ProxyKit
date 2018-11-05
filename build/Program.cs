using System;

namespace build
{
    class Program
    {
        static void Main(string[] args)
        {
            Target("default", () => System.Console.WriteLine("Hello, world!"));
            RunTargets(args);
        }
    }
}
