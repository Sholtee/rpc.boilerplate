using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Tests.Base
{
    public class Debuggable
    {
        public Debuggable()
        {
            if (Environment.GetEnvironmentVariable("TARGET") == Path.GetFileName(GetType().Assembly.Location))
            {
                Console.Out.WriteLine("Waiting for the debugger...");
                SpinWait.SpinUntil(() => Debugger.IsAttached);
            }
        }
    }
}
    