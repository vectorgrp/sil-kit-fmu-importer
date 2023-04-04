namespace FmuImporter.Config;

public class Configuration
{
  public class Parameter
  {
    public string VarName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public object? Value { get; set; }

    public Parameter()
    {
      VarName = string.Empty;
    }
  }

  public class ParameterSet
  {
    public class IncludeParameterSet
    {
      public string? RefName { get; set; }
      public string? FilePath { get; set; }
    }

    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Parameter> Parameters;
    public List<IncludeParameterSet>? IncludeParameterSets;

    public ParameterSet()
    {
      Parameters = new List<Parameter>();
      IncludeParameterSets = new List<IncludeParameterSet>();
    }
  }

  public string? Description { get; set; }
  public List<ParameterSet>? ParameterSets { get; set; }

  public string? ConfigurationPath { get; set; }

  public Dictionary<string, Parameter> GetParameters(string? parameterSetName)
  {
    var dict = new Dictionary<string, Parameter>();
    UpdateParameterDictionary(parameterSetName, ref dict);
    return dict;
  }

  public void UpdateParameterDictionary(string? parameterSetName, ref Dictionary<string, Parameter> parameterDictionary)
  {
    if (parameterSetName == null)
    {
      return;
    }

    var paramSet = ParameterSets?.FirstOrDefault(e => e.Name == parameterSetName);
    if (paramSet == null)
    {
      return;
    }

    // Fill parameter list with parameters from included sets before local parameters
    if (paramSet.IncludeParameterSets != null)
    {
      for (var i = 0; i < paramSet.IncludeParameterSets.Count; i++)
      {
        var includeParameterSet = paramSet.IncludeParameterSets[i];
        if (includeParameterSet.RefName == null)
        {
          // TODO do this outside of the configuration class
          // Warn that an invalid parameter set was included and that it is skipped - include index of paramSet
          continue;
        }

        if (includeParameterSet.FilePath == null)
        {
          // TODO use dictionary and create or replace
          UpdateParameterDictionary(includeParameterSet.RefName, ref parameterDictionary);
        }
        else
        {
          if (Path.IsPathFullyQualified(includeParameterSet.FilePath))
          {
            if (!File.Exists(includeParameterSet.FilePath))
            {
              throw new FileNotFoundException("");
            }

            ConfigParser.LoadConfiguration(includeParameterSet.FilePath);
          }
          else
          {
            if (ConfigurationPath == null)
            {
              throw new InvalidOperationException("ConfigurationPath not set");
            }

            var configDir = Path.GetDirectoryName(ConfigurationPath);
            if (configDir == null)
            {
              throw new InvalidOperationException("Failed to retrieve directory from current config file path");
            }

            var includedFile = Path.Combine(configDir, includeParameterSet.FilePath);
            if (!File.Exists(includedFile))
            {
              throw new FileNotFoundException("");
            }

            var recConfig = ConfigParser.LoadConfiguration(includedFile);
            recConfig.UpdateParameterDictionary(includeParameterSet.RefName, ref parameterDictionary);
          }
        }
      }
    }

    // Add locally defined parameters after all parameter sets were read
    foreach (var parameter in paramSet.Parameters)
    {
      // Create or update parameter value
      parameterDictionary[parameter.VarName] = parameter;
    }
  }
}