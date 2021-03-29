
using ServiceStack.DataAnnotations;

namespace DAL
{
    using Services.API;

    [DataTable]
    public class User 
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [References(typeof(DAL.Login)), Index(Unique = true)]
        public long LoginId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;
    }
}
