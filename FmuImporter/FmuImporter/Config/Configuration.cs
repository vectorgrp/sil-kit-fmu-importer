namespace FmuImporter.Config;

public class Configuration : ConfigurationInternal
{
  public string? ConfigurationPath { get; set; }

  public Dictionary<string, Parameter> GetParameters()
  {
    var dict = new Dictionary<string, Parameter>();
    UpdateParameterDictionary(ref dict);
    return dict;
  }

  private void UpdateParameterDictionary(ref Dictionary<string, Parameter> parameterDictionary)
  {
    if (ParameterSet == null)
    {
      // the configuration does not have a defined parameter set
      return;
    }

    // Fill parameter list with parameters from included sets before local parameters
    if (ParameterSet.IncludeParameterSets != null)
    {
      for (var i = 0; i < ParameterSet.IncludeParameterSets.Count; i++)
      {
        var includeParameterSet = ParameterSet.IncludeParameterSets[i];
        if (string.IsNullOrEmpty(includeParameterSet))
        {
          // TODO log warning about missing file path and skip
          continue;
        }
        else
        {
          if (Path.IsPathFullyQualified(includeParameterSet))
          {
            if (!File.Exists(includeParameterSet))
            {
              // TODO log warning about invalid file path and skip
              continue;
            }

            ConfigParser.LoadConfiguration(includeParameterSet);
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

            var includedFile = Path.Combine(configDir, includeParameterSet);
            if (!File.Exists(includedFile))
            {
              // TODO log warning about invalid file path and skip
              continue;
            }

            var recConfig = ConfigParser.LoadConfiguration(includedFile);
            recConfig.UpdateParameterDictionary(ref parameterDictionary);
          }
        }
      }
    }

    if (ParameterSet.Parameters == null)
    {
      return;
    }

    // Add locally defined parameters after all parameter sets were read
    foreach (var parameter in ParameterSet.Parameters)
    {
      // Create or update parameter value
      parameterDictionary[parameter.VarName] = parameter;
    }
  }
}