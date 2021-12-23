namespace Services.API
{
    [ConfigNode("Database")]
    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = string.Empty;

        public string MigrationDir { get; set; } = string.Empty;
    }
}
