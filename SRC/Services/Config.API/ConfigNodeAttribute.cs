using System;

namespace Services.API
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ConfigNodeAttribute : Attribute
    {
        public string Node { get; }

        public ConfigNodeAttribute(string node) => Node = node ?? throw new ArgumentNullException(nameof(node));
    }
}
