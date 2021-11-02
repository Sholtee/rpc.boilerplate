using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Solti.Utils.Rpc.Internals;

namespace Server
{
    internal sealed class TraceLogger : LoggerBase
    {
        protected override void LogCore(string message) => Trace.WriteLine(message);

        public static ILogger Create<TCategory>() => new TraceLogger(GetDefaultCategory<TCategory>());

        public TraceLogger(string category) : base(category) { }
    }
}
