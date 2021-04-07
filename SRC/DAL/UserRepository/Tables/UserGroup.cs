using System;

using ServiceStack.DataAnnotations;

using Solti.Utils.OrmLite.Extensions;

namespace DAL
{
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
