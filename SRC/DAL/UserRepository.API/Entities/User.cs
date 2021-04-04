using System;

using Solti.Utils.Rpc.Interfaces;

namespace DAL.API
{
    public class User
    {
        public Guid? Id { get; set; }
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [NotNull, LengthBetween(min: 5)]
        public string EmailOrUserName { get; set; }
        [NotNull, LengthBetween(min: 5)]
        public string FullName { get; set; }
        #pragma warning restore CS8618
    }
}
