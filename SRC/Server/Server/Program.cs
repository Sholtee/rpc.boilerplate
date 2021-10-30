using Solti.Utils.Rpc.Hosting;

namespace Server
{
    public static class Program
    {
        static void Main() => HostRunner.Run<AppHost>();
    }
}