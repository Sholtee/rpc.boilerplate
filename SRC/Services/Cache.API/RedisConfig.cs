namespace Services.API
{
    [ConfigNode("Redis")]
    public class RedisConfig 
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
