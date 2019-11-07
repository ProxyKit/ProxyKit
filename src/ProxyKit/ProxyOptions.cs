using System;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    public class ProxyOptions
    {
        public TimeSpan? WebSocketKeepAliveInterval { get; set; }

        public int? WebSocketBufferSize { get; set; }
        
        /// <summary>
        /// Determines the condition for copying request body to upstream. It is always `true` and copies regardless if not set.
        /// </summary>
        public Func<HttpRequest, bool> CopyRequestBodyIf { get; set; } = request => true;
    }
}
