using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Services
{
    using API;
    using Properties;

    public class Config<TNode> : IConfig<TNode>
    {
        private static TNode Read(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            string node = typeof(TNode).GetCustomAttribute<ConfigNodeAttribute>()?.Node ?? throw new InvalidOperationException(Resources.NOT_A_CONFIG_ENTITY);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.GetString() != node)
                    {
                        if (!reader.Read())
                            break;

                        if (new[] { JsonTokenType.StartObject, JsonTokenType.StartArray }.Contains(reader.TokenType))
                            reader.Skip();

                        continue;
                    }

                    TNode? result = (TNode?) JsonSerializer.Deserialize(ref reader, typeof(TNode), options: null);
                    return result ?? throw new JsonException();
                }
            }

            throw new JsonException();
        }
    

        public TNode Value { get; }

        public Config(string configFile) 
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Config<>).Assembly.Location)!, configFile);

            Utf8JsonReader reader = new
            (
                Encoding.UTF8.GetBytes
                (
                    File.ReadAllText(path)
                )
            );

            reader.Read();

            Value = Read(ref reader);
        }
    }
}
