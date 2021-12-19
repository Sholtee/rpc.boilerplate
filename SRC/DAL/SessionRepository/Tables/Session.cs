using System;

using ServiceStack.DataAnnotations;

using Solti.Utils.OrmLite.Extensions;

namespace DAL
{
    [DataTable]
    class Session
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        [Required, Index]
        public Guid UserId { get; set; }

        [Required]
        public DateTime CreatedUtc { get; set; }

        [Required]
        public DateTime ExpiresUtc { get; set; }
    }
}
