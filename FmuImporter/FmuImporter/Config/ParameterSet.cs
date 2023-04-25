namespace FmuImporter.Config;

public class ParameterSet
{
  public class IncludeParameterSet
  {
    public string? FilePath { get; set; }
  }

  public string? Description { get; set; }
  public List<Parameter>? Parameters;
  public List<string>? IncludeParameterSets;

  public ParameterSet()
  {
    Parameters = new List<Parameter>();
    IncludeParameterSets = new List<string>();
  }
}