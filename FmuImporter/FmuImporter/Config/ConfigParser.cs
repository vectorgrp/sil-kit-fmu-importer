// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;
using FmuImporter.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace FmuImporter.Config;

public static class ConfigParser
{
  private class ValidatingNodeDeserializer : INodeDeserializer
  {
    private readonly INodeDeserializer _nodeDeserializer;

    public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer)
    {
      _nodeDeserializer = nodeDeserializer;
    }

    public bool Deserialize(
      IParser reader,
      Type expectedType,
      Func<IParser, Type, object?> nestedObjectDeserializer,
      out object? value)
    {
      var success = _nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
      if (success)
      {
        var context = new ValidationContext(value!, null, null);
        Validator.ValidateObject(value!, context);
      }

      return success;
    }
  }

  public static Configuration LoadConfiguration(string path)
  {
    var deserializer =
      new DeserializerBuilder()
        .WithNodeDeserializer(
          inner => new ValidatingNodeDeserializer(inner),
          s => s.InsteadOf<ObjectNodeDeserializer>())
        .Build();

    Configuration? config;
    try
    {
      config = deserializer.Deserialize<Configuration?>(File.ReadAllText(path));
      if (config == null)
      {
        throw new InvalidConfigurationException("Failed to deserialize the provided FMU configuration file");
      }
    }
    catch (ValidationException e)
    {
      throw new InvalidConfigurationException("The configuration is invalid.", e);
    }

    config.ConfigurationPath = Path.GetFullPath(path);
    ValidateConfig(config);
    return config;
  }

  private static void ValidateConfig(Configuration config)
  {
  }
}
