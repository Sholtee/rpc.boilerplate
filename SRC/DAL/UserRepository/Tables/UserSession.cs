using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    class UserSession
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
