using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    public class Group
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Required, Index(Unique = true), StringLength(minimumLength: 5, maximumLength: 100)]
        public string Name { get; set; }
        #pragma warning restore CS8618

        public Roles Roles { get; set; }
    }
}
