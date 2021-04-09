using System.Reflection;

using Solti.Utils.Rpc.Interfaces;

namespace Services.API
{
    [ParameterValidatorAspect]
    public interface IDbSchemaManager
    {
        void CreateTables([NotNull] params Assembly[] asmsToSearch);

        void DropTables([NotNull] params Assembly[] asmsToSearch);
    }
}
