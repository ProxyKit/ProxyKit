using System;

namespace ProxyKit.Testing
{
    public sealed class Origin
    {
        public string Host { get; }

        public uint Port { get; }

        public Origin(string host, uint port)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        private bool Equals(Origin other)
        {
            return string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Origin other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(Host) * 397) ^ (int)Port;
            }
        }

        public static bool operator ==(Origin left, Origin right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Origin left, Origin right)
        {
            return !Equals(left, right);
        }
    }
}