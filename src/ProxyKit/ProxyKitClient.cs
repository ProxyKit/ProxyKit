using System.Net.Http;

namespace ProxyKit
{
    public class ProxyKitClient
    {
        internal HttpClient Client { get; }
        public const string Key = "ProxyKitClient";

        public ProxyKitClient(HttpClient client)
        {
            Client = client;
        }
    }
}
