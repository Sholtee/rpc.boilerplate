using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    class UserSession
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [References(typeof(DAL.User))]
        public long UserId { get; set; }

        [Index(Unique = true), Required]
        public Guid SessionId { get; set; }

        [Required]
        public DateTime CreatedUtc { get; set; }

        [Required]
        public DateTime ExpiredUtc { get; set; }
    }
}
