using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    public class UserGroup 
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        [References(typeof(User))]
        public Guid UserId { get; set; }

        [References(typeof(Group))]
        public Guid GroupId { get; set; }
    }
}
