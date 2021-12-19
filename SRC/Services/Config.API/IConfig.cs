namespace Services.API
{
    public interface IConfig
    {
        ServerConfig Server { get; }
        
        DatabaseConfig Database { get; }

        RedisConfig Redis { get; }
    }
}
