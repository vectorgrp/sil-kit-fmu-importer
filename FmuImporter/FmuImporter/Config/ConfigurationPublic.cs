namespace FmuImporter.Config;

public class ConfigurationPublic
{
  public int? Version { get; set; }
  public string? Description { get; set; }
  public UInt64? StepSize { get; set; }
  public List<string/* includePath */>? Include { get; set; }
  public List<Parameter>? Parameters { get; set; }
  public List<ConfiguredVariable>? VariableMappings { get; set; }
  public bool? IgnoreUnmappedVariables { get; set; }
}
