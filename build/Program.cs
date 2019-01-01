using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    class Program
    {
        private const string ArtifactsDir = "artifacts";
        private const string Clean = "clean";
        private const string Build = "build";
        private const string RunTests = "run-tests";
        private const string Pack = "pack";
        private const string Publish = "publish";

        static void Main(string[] args)
        {
            Target(Build, () => Run("dotnet", "build ProxyKit.sln -c Release"));

            Target(
                RunTests,
                DependsOn(Build),
                () => Run("dotnet", $"test src/ProxyKit.Tests/ProxyKit.Tests.csproj -c Release -r ../../{ArtifactsDir} --no-build -l trx;LogFileName=ProxyKit.Tests.xml --verbosity=normal"));

            Target(
                Pack,
                DependsOn(Build),
                () => Run("dotnet", $"pack src/ProxyKit/ProxyKit.csproj -c Release -o ../../{ArtifactsDir} --no-build"));

            Target(Publish, DependsOn(Pack), () =>
            {
                var packagesToPush = Directory.GetFiles(ArtifactsDir, "*.nupkg", SearchOption.TopDirectoryOnly);
                Console.WriteLine($"Found packages to publish: {string.Join("; ", packagesToPush)}");

                var apiKey = Environment.GetEnvironmentVariable("MYGET_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine("MyGet API key not available. Packages will not be pushed.");
                    return;
                }

                foreach (var packageToPush in packagesToPush)
                {
                    try
                    {
                        Run("dotnet", $"nuget push {packageToPush} -s https://www.myget.org/F/dh/api/v3/index.json -k {apiKey}", noEcho: true);
                    }
                    catch(SimpleExec.NonZeroExitCodeException){} //can get 1 if try to push package that differs only in build metadata
                }
            });

            Target("default", DependsOn(RunTests, Publish));

            RunTargetsAndExit(args);
        }
    }
}
