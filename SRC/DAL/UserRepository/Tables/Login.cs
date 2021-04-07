using System;

using ServiceStack.DataAnnotations;

using Solti.Utils.OrmLite.Extensions;

namespace DAL
{
    [DataTable]
    public class Login
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Required, Index, StringLength(minimumLength: 5, maximumLength: int.MaxValue)]
        public string EmailOrUserName { get; set; }

        [Required]
        public string PasswordHash { get; set; }
        #pragma warning restore CS8618

        public DateTime? DeletedUtc { get; set; }
    }
}
