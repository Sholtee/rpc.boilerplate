using System;

using Solti.Utils.Rpc.Interfaces;

namespace DAL.API
{
    using Services.API;

    public class User
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [NotNull, LengthBetween(min: 5)]
        public string EmailOrUserName { get; set; }
        [NotNull, LengthBetween(min: 5)]
        public string FullName { get; set; }
        #pragma warning restore CS8618    
    }

    #pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class UserEx: User
    #pragma warning restore CA1711
    {
        public Guid Id { get; set; }
        public Roles Roles { get; set; }
    }
}
