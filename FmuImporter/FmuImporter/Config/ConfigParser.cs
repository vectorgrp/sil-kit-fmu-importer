namespace FmuImporter.Config;

public static class ConfigParser
{
  public static Configuration LoadConfiguration(string path)
  {
    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
      .Build();

    var config = deserializer.Deserialize<Configuration>(File.ReadAllText(path));
    config.ConfigurationPath = path;
    return config;
  }
}