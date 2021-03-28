using System.Text.Json;
using System.IO;

#pragma warning disable CS8618

namespace Services
{
    using API;

    public class Config : IConfig
    {
        public ServerConfig Server { get; set; }

        public string ConnectionString { get; set; }

        public RedisConfig Redis { get; set; }

        public static Config Read(string configFile) 
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Config).Assembly.Location)!, configFile);

            return JsonSerializer.Deserialize<Config>(File.ReadAllText(path))!;
        }
    }
}
