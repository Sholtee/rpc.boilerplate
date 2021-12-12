using Solti.Utils.Rpc.Interfaces;

namespace Services.API
{
    [TransactionAspect]
    public interface IInstaller 
    {
        [Transactional]
        void Install(InstallArguments args);

        [Transactional]
        void Migrate(string migrationFilesDir);

        string Status { get; }
    }
}
