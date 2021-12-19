using System;

using ServiceStack.DataAnnotations;

using Solti.Utils.OrmLite.Extensions;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable

namespace DAL
{
    [DataTable]
    public class User 
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        [Required, StringLength(minimumLength: 5, maximumLength: int.MaxValue)]
        public string FullName { get; set; }

        [Required, Index, StringLength(minimumLength: 5, maximumLength: int.MaxValue)]
        public string EmailOrUserName { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime? DeletedUtc { get; set; }
    }
}
