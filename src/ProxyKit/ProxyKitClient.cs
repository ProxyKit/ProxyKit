using System.Net.Http;

namespace ProxyKit
{
    internal class ProxyKitClient
    {
        internal HttpClient Client { get; }

        public ProxyKitClient(HttpClient client)
        {
            Client = client;
        }
    }
}