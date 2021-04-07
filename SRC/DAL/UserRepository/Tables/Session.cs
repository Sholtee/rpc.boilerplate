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

        [References(typeof(DAL.User))]
        public Guid UserId { get; set; }

        [Required]
        public DateTime CreatedUtc { get; set; }

        [Required]
        public DateTime ExpiredUtc { get; set; }
    }
}
