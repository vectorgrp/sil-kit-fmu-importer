namespace FmuImporter.Config;

public class ParameterSet
{
  public string? Description { get; set; }
  public List<Parameter>? Parameters;

  public ParameterSet()
  {
    Parameters = new List<Parameter>();
  }
}
