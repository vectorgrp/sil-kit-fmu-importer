// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Exceptions;

namespace FmuImporter.Config;

public static class ConfigParser
{
  public static Configuration LoadConfiguration(string path)
  {
    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
      .Build();

    var config = deserializer.Deserialize<Configuration>(File.ReadAllText(path));
    config.ConfigurationPath = path;
    config.IgnoreUnmappedVariables = config.IgnoreUnmappedVariables ?? false;
    ValidateConfig(config);
    return config;
  }

  private static void ValidateConfig(Configuration config)
  {
    if (config.Version == null || config.Version == 0)
    {
      throw new InvalidConfigurationException(
        $"The loaded configuration is missing a Version field. Path: {config.ConfigurationPath}");
    }

    if( config.VariableMappings != null)
    {
      foreach( var Var in config.VariableMappings)
      {
        var transf = Var.Transformation;

        if(transf != null && transf.Factor != null && transf.Factor == 0.0)
        {
          throw new InvalidConfigurationException(
       $"The loaded transformation for {nameof(Var.VariableName)} has a factor of zero. Path: {config.ConfigurationPath}");
        }
      }
    }
  }
}
