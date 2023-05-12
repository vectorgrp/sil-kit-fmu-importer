namespace FmuImporter.Config;

public class ConfigurationPublic
{
  public int? Version { get; set; }
  public string? Description { get; set; }
  public List<string/* includePath */>? Include { get; set; }
  public ParameterSet? ParameterSet { get; set; }
  public VariableConfiguration? VariableConfiguration { get; set; }
}
