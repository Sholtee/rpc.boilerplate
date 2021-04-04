﻿using System;

using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    public class User 
    {
        [PrimaryKey, AutoId]
        public Guid Id { get; set; }

        [References(typeof(DAL.Login)), Index(Unique = true)]
        public Guid LoginId { get; set; }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Required]
        public string FullName { get; set; }
        #pragma warning restore CS8618
    }
}
