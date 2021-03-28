using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Services.API
{
    public class ServerConfig 
    {
        public string Host { get; set; } = string.Empty;

        [SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
        public IList<string> AllowedOrigins { get; set; } = new List<string>();

        public int SessionTimeoutInMinutes { get; set; }
    }
}
