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
      try
      {
        var success = _nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
        if (success)
        {
          try
          {
            var context = new ValidationContext(value!, null, null);
            Validator.ValidateObject(value!, context, true);
          }
          catch (ValidationException e)
          {
            ;
            //success = false;
            throw new InvalidConfigurationException($"The configuration is invalid: {e.Message}", e);
          }
          catch (Exception)
          {
            throw;
          }
        }

        return success;
      }
      catch (InvalidConfigurationException)
      {
        throw;
      }
      catch (YamlException e)
      {
        if (e.InnerException != null)
        {
          throw new InvalidConfigurationException($"{e.InnerException.Message}", e);
        }
        else
        {
          throw new InvalidConfigurationException($"The configuration is invalid. Parser reported: {e.Message}", e);
        }
      }
      catch (Exception e)
      {
        throw new InvalidConfigurationException($"The configuration is invalid: {e.Message}", e);
      }
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
    catch (Exception e)
    {
      var currentException = e;
      var continueDescending = currentException.InnerException != null && currentException is not InvalidConfigurationException;

      while (continueDescending)
      {
        if (currentException is YamlException yamlException)
        {
          if (currentException.InnerException != null && currentException.InnerException is YamlException or InvalidConfigurationException)
          {
            currentException = currentException.InnerException;
            continue;
          }

          currentException = new InvalidConfigurationException(
            $"Invalid configuration. Issue detected between {yamlException.Start} and {yamlException.End}.",
            yamlException);
        }
        else
        {
          currentException = new InvalidConfigurationException(
            $"Invalid configuration. {currentException.Message}", currentException);
        }
        continueDescending = false;
      }

      throw currentException;
    }

    config.ConfigurationPath = Path.GetFullPath(path);
    ValidateConfig(config);
    return config;
  }

  private static void ValidateConfig(Configuration config)
  {
  }
}
