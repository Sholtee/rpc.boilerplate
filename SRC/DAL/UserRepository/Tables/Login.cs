using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    public class Login
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [References(typeof(User)), Index(Unique = true)]
        public long UserId { get; set; }

        [Required, Index]
        public string EmailOrUserName { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime? Deleted { get; set; }
    }
}
