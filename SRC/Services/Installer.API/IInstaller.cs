using System.Collections.Generic;

using Solti.Utils.Rpc.Interfaces;

namespace Services.API
{
    [TransactionAspect]
    public interface IInstaller 
    {
        [Transactional]
        void Install(InstallArguments args);

        [Transactional]
        IEnumerable<string> Migrate();

        string Status { get; }
    }
}
