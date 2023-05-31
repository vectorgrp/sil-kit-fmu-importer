using System.Text;

namespace Fmi.FmiModel.Internal
{
  public class ModelDescription
  {
    private Dictionary<uint, Variable> variables;

    // Former attributes
    public string ModelName { get; set; }
    public string Description { get; set; }
    public string InstantiationToken { get; set; }
    public string FmiVersion { get; set; }
    public string Version { get; set; }


    // Former nodes
    public CoSimulation CoSimulation { get; set; }
    public DefaultExperiment DefaultExperiment { get; set; }

    public Dictionary<uint /* ValueReference */, Variable> Variables
    {
      get { return variables; }
      set { variables = value; }
    }

    public ModelDescription(Fmi3.fmiModelDescription input)
    {
      // init of local fields & properties
      variables = new Dictionary<uint, Variable>();

      // Attribute init
      ModelName = input.modelName;
      Description = input.description;
      InstantiationToken = input.instantiationToken.Normalize();
      FmiVersion = input.fmiVersion;
      Version = input.version;

      // Node init
      CoSimulation = new CoSimulation(input.CoSimulation);
      DefaultExperiment = new DefaultExperiment(input.DefaultExperiment);
      InitVariableMap(input.ModelVariables);
    }

    public ModelDescription(Fmi2.fmiModelDescription input)
    {
      // init of local fields & properties
      Variables = new Dictionary<uint, Variable>();

      // Attribute init
      ModelName = input.modelName;
      Description = input.description;
      InstantiationToken = input.guid.Normalize();
      FmiVersion = input.fmiVersion;
      Version = input.version;

      // Node init
      if (input.CoSimulation.Length < 1)
      {
        throw new ArgumentOutOfRangeException(
            $"The model description does not provide a CoSimulation description.");
      }
      CoSimulation = new CoSimulation(input.CoSimulation[0]);
      DefaultExperiment = new DefaultExperiment(input.DefaultExperiment);
      InitVariableMap(input.ModelVariables);
    }

    private void InitVariableMap(Fmi3.fmiModelDescriptionModelVariables input)
    {
      foreach (var fmiModelDescriptionModelVariable in input.Items)
      {
        var v = new Variable(fmiModelDescriptionModelVariable);
        var result = Variables.TryAdd(v.ValueReference, v);
        if (!result)
        {
          throw new ArgumentException(
              "Failed to parse model description: multiple variables have the same valueReference.");
        }
      }
      // iterate through all variables again and initialize array length
      foreach (var variable in Variables)
      {
        variable.Value.InitializeArrayLength(ref variables);
      }
    }

    private void InitVariableMap(Fmi2.fmiModelDescriptionModelVariables input)
    {
      foreach (var fmiModelDescriptionModelVariable in input.ScalarVariable)
      {
        var v = new Variable(fmiModelDescriptionModelVariable);
        var result = Variables.TryAdd(v.ValueReference, v);
        if (!result)
        {
          throw new ArgumentException(
              "Failed to parse model description: multiple variables have the same valueReference.");
        }
      }
    }

    public override string ToString()
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.AppendLine("ModelName: " + ModelName);
      stringBuilder.AppendLine("FMI version: " + FmiVersion);
      stringBuilder.AppendLine("Description: " + Description);

      stringBuilder.AppendLine();
      foreach (var variable in Variables.Values)
      {
        stringBuilder.AppendLine(variable.ToString());
      }

      return stringBuilder.ToString();
    }
  }
}
