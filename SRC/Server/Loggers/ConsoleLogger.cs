using System;

using Microsoft.Extensions.Logging;

using Solti.Utils.Rpc.Internals;

namespace Server.Loggers
{
    public sealed class ConsoleLogger : LoggerBase
    {
        protected override void LogCore(string message) => Console.Out.WriteLine(message);

        public static ILogger Create<TCategory>() => new ConsoleLogger(GetDefaultCategory<TCategory>());

        public ConsoleLogger(string category) : base(category) { }
    }
}
