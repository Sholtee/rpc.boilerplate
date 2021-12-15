using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Tests.Base
{
    public class Debuggable
    {
        public Debuggable()
        {
            string? target = Environment.GetEnvironmentVariable("TARGET");

            if (target is not null && GetType().Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(meta => meta.Key == "ProjectFile" && meta.Value == target))
            {
                Console.Out.WriteLine("Waiting for the debugger...");
                SpinWait.SpinUntil(() => Debugger.IsAttached);
            }
        }
    }
}
    