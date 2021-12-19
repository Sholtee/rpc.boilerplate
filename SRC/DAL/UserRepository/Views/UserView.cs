using System;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CA1812

namespace DAL
{
    using Services.API;

    internal class UserView
    {
        public Guid Id { get; set; }    

        public string PasswordHash { get; set; }

        public string EmailOrUserName { get; set; }

        public string FullName { get; set; }

        public Roles Roles { get; set; }
    }
}
