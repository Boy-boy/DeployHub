using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace ImageUploaderApi.Infrastructure
{

    public interface IYamlSerializer
    {
        public string Serialize<T>(T obj);

        public T Deserialize<T>(string yaml);
    }

    public class YamlSerializer : IYamlSerializer
    {
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public YamlSerializer()
        {
            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public string Serialize<T>(T obj) => _serializer.Serialize(obj);
        public T Deserialize<T>(string yaml) => _deserializer.Deserialize<T>(yaml);
    }
}
