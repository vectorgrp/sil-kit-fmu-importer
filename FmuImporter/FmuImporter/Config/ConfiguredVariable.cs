using Fmi.FmiModel.Internal;

namespace FmuImporter.Config;

public class ConfiguredVariable
{

  public string? VariableName { get; set; }
  public string? TopicName { get; set; }
  public Variable? FmuVariableDefinition { get; set; }
  public Transformation? Transformation { get; set; }

  // internal data
  public object? SilKitService { get; set; }
}