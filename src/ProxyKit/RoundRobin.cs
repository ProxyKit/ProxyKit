using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProxyKit
{
    /// <summary>
    ///     Represents a round robing collection of hosts.
    /// </summary>
    public class RoundRobin
    {
        private readonly UpstreamHost[] _hosts;
        private int _position;

        public RoundRobin(params UpstreamHost[] hosts)
        {
            _hosts = hosts.ToArray();
        }

        public UpstreamHost Next()
        {
            if (_hosts.Length == 1)
            {
                return _hosts[0];
            }

            Interlocked.Increment(ref _position);
            var mod = _position % _hosts.Length;
            return _hosts[mod];
        }
    }
}
