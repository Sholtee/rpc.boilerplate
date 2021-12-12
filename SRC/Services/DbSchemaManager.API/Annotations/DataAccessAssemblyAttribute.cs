using System;

namespace Services.API
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class DataAccessAssemblyAttribute: Attribute
    {
    }
}
