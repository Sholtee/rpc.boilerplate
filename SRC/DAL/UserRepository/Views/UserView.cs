using System;
using System.Diagnostics.CodeAnalysis;

namespace DAL
{
    [SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "ORMLite instantiates this class")]
    internal class UserView
    {
        public Guid Id { get; set; }
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string PasswordHash { get; set; }
        public string EmailOrUserName { get; set; }
        public string FullName { get; set; }
        #pragma warning restore CS8618 
    }
}
