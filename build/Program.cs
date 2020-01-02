using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    class Program
    {
        private const string ArtifactsDir = "artifacts";
        private const string Build = "build";
        private const string Test = "test";
        private const string Pack = "pack";
        private const string Publish = "publish";

        static void Main(string[] args)
        {
            Target(Build, () => Run("dotnet", "build ProxyKit.sln -c Release"));

            Target(
                Test,
                DependsOn(Build),
                () => Run("dotnet", $"test src/ProxyKit.Tests/ProxyKit.Tests.csproj -c Release -r {ArtifactsDir} --no-build -l trx;LogFileName=ProxyKit.Tests.xml --verbosity=normal"));

            Target(
                Pack,
                DependsOn(Build),
                () => Run("dotnet", $"pack src/ProxyKit/ProxyKit.csproj -c Release -o {ArtifactsDir} --no-build"));

            Target(Publish, DependsOn(Pack), () =>
            {
                var packagesToPush = Directory.GetFiles(ArtifactsDir, "*.nupkg", SearchOption.TopDirectoryOnly);
                Console.WriteLine($"Found packages to publish: {string.Join("; ", packagesToPush)}");

                var feedzApiKey = Environment.GetEnvironmentVariable("FEEDZ_PROXYKIT_API_KEY");
                if (!string.IsNullOrWhiteSpace(feedzApiKey))
                {
                    Console.WriteLine("Feedz API Key availabile. Pushing packages to Feedz...");
                    foreach (var packageToPush in packagesToPush)
                    {
                        // NOTE: the try catch can be removed when https://github.com/NuGet/Home/issues/1630 is released.
                        try
                        {
                            Run("dotnet", $"nuget push {packageToPush} -s https://f.feedz.io/dh/oss-ci/nuget/index.json -k {feedzApiKey}", noEcho: true);
                        }
                        catch (SimpleExec.NonZeroExitCodeException) { } //can get 1 if try to push package that differs only in build metadata
                    }
                }
            });

            Target("default", DependsOn(Test, Publish));

            RunTargetsAndExit(args);
        }
    }
}
