namespace FmuImporter.Config;

public class VariableConfiguration
{
  public bool? UseAsWhitelist { get; set; }
  public List<ConfiguredVariable> Mappings { get; set; }

  public VariableConfiguration()
  {
    UseAsWhitelist = false;
    Mappings = new List<ConfiguredVariable>();
  }
}

