using System;

namespace Services.API
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DataTableAttribute: Attribute
    {
    }
}
