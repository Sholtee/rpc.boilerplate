using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    public class Login
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Required, Index]
        public string EmailOrUserName { get; set; }

        [Required]
        public string PasswordHash { get; set; }
        #pragma warning restore CS8618

        public DateTime? Deleted { get; set; }
    }
}
