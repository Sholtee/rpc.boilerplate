using System.Collections.Generic;

namespace DAL.API
{
    public class PartialUserList
    {
        #pragma warning disable CA2227 // Collection properties should be read only
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IList<User> Entries { get; set; }
        #pragma warning restore CS8618
        #pragma warning restore CA2227

        public long AllEntries { get; set; }
    }
}
