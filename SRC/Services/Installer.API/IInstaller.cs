using System.Reflection;

using Solti.Utils.Rpc.Interfaces;

namespace Services
{
    [ParameterValidatorAspect, TransactionAspect]
    public interface IInstaller 
    {
        [Transactional]
        void Run([NotNull] Assembly hostAssembly);
    }
}
