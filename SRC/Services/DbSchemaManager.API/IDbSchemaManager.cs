using System.Reflection;

using Solti.Utils.Rpc.Interfaces;

namespace Services.API
{
    [ParameterValidatorAspect, TransactionAspect]
    public interface IDbSchemaManager
    {
        [Transactional]
        void CreateTables([NotNull] params Assembly[] asmsToSearch);

        [Transactional]
        void DropTables([NotNull] params Assembly[] asmsToSearch);
    }
}
