namespace FmuImporter.Config;

public class InvalidConfigurationException : Exception
{
  public InvalidConfigurationException(string message) : base(message)
  {
  }
}
