using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ProxyKit
{
    internal static class WebHostExtensions
    {
        internal static int GetServerPort(this IWebHost server)
        {
            var address = server.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
            var match = Regex.Match(address, @"^.+:(\d+)$");
            var port = 0;

            if (match.Success)
            {
                port = int.Parse(match.Groups[1].Value);
            }

            return port;
        }
    }
}