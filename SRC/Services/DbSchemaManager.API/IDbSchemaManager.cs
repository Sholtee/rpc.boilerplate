using System;

using Solti.Utils.Rpc.Interfaces;

namespace Services.API
{
    [ParameterValidatorAspect]
    public interface IDbSchemaManager
    {
        void Initialize();

        void Drop();

        bool IsInitialized { get; }

        DateTime GetLastMigrationUtc();

        bool Migrate(DateTime createdAtUtc, [NotNull] string sql, string? comment = null);
    }
}
