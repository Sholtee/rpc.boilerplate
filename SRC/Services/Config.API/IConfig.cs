namespace Services.API
{
    public interface IConfig
    {
        ServerConfig Server { get; }
        string ConnectionString { get; } 
        RedisConfig Redis { get; }
    }
}
