namespace FmuImporter.Config;

public class ConfigurationInternal
{
  public string? Description { get; set; }
  public ParameterSet? ParameterSet { get; set; }
  public List<ConfiguredVariable>? ConfiguredVariables { get; set; }
}
