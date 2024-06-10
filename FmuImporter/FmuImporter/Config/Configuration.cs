// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Security.Cryptography;
using FmuImporter.Exceptions;
using SilKit.Services.Logger;

namespace FmuImporter.Config;

public class Configuration : ConfigurationPublic
{
  public string? ConfigurationPath { get; set; }

  internal LinkedList<Configuration>? AllConfigurations { get; set; }

  private Dictionary<string, Parameter>? _resolvedParameters;
  private Dictionary<string, VariableConfiguration>? _resolvedVariableConfigurations;

  internal Dictionary<string, Parameter> ResolvedParameters
  {
    get
    {
      if (_resolvedParameters == null)
      {
        _resolvedParameters = new Dictionary<string, Parameter>();
        UpdateParameterDictionary(_resolvedParameters);
      }

      return _resolvedParameters;
    }
  }

  internal Dictionary<string, VariableConfiguration> ResolvedVariableConfigurations
  {
    get
    {
      if (_resolvedVariableConfigurations == null)
      {
        _resolvedVariableConfigurations = new Dictionary<string, VariableConfiguration>();
        UpdateVariablesDictionary(_resolvedVariableConfigurations);
      }

      return _resolvedVariableConfigurations;
    }
  }

  private ILogger? SilKitLogger { get; set; }

  public void SetSilKitLogger(ILogger? logger)
  {
    SilKitLogger = logger;
  }

  /// <summary>
  ///   This method uses a breadth-first approach read all configuration files that were included by the current
  ///   configuration, then in the included configurations, etc.
  ///   Configurations that were loaded by a previous configuration will be skipped (breaking loops).
  ///   After all configurations were loaded, their parameters and mapped variables are merged. Already existing values are
  ///   overridden (last-is-best approach).
  ///   The configurations with the lowest depth are handled last (~ locally configured attributes take precedence to
  ///   included attributes).
  /// </summary>
  /// <exception cref="FileNotFoundException">
  ///   Thrown if the path in the include block of a configuration did not point to a
  ///   file.
  /// </exception>
  public void MergeIncludes()
  {
    var configHashes = new HashSet<string>();
    AllConfigurations = new LinkedList<Configuration>();
    var currentNode = AllConfigurations.AddFirst(this);

    if (ConfigurationPath == null)
    {
      return;
    }

    var fullPath = Path.GetFullPath(ConfigurationPath);
    if (!File.Exists(fullPath))
    {
      throw new FileNotFoundException($"The configuration file '{ConfigurationPath}' was not initialized properly.");
    }

    var hashValue = Sha512CheckSum(fullPath);
    configHashes.Add(hashValue);

    do
    {
      var newConfigs = currentNode.ValueRef.MergeIncludes(configHashes, currentNode.Value);
      if (newConfigs != null)
      {
        foreach (var newConfig in newConfigs)
        {
          AllConfigurations.AddLast(newConfig);
        }
      }

      currentNode = currentNode.Next;
    } while (currentNode != null);
  }

  private List<Configuration>? MergeIncludes(HashSet<string> configHashes, Configuration currentConfiguration)
  {
    var includes = currentConfiguration.Include;
    if (includes == null)
    {
      return null;
    }

    var result = new List<Configuration>();
    foreach (var includePath in includes)
    {
      var fullPath = GetFullPath(includePath);
      if (!File.Exists(fullPath))
      {
        throw new FileNotFoundException(
          $"The file '{includePath}' included in '{ConfigurationPath}' was not found. Searched in '{fullPath}.");
      }

      var hashValue = Sha512CheckSum(fullPath);
      if (configHashes.Contains(hashValue))
      {
        // skip already existing configurations
        continue;
      }

      var config = ConfigParser.LoadConfiguration(fullPath);
      config.SetSilKitLogger(SilKitLogger);
      configHashes.Add(hashValue);
      result.Add(config);
    }

    return result;
  }

  public Dictionary<string, Parameter> GetParameters()
  {
    return ResolvedParameters;
  }

  public Dictionary<string, VariableConfiguration> GetVariables()
  {
    return ResolvedVariableConfigurations;
  }

  private void UpdateParameterDictionary(Dictionary<string, Parameter> parameterDictionary)
  {
    if (AllConfigurations == null)
    {
      throw new NullReferenceException("AllConfigurations was not initialized.");
    }

    if (AllConfigurations.Count == 0)
    {
      return;
    }

    // Last cannot be null here -> omit nullable
    // NB: The last handled entry of AllConfigurations is the configuration itself -> local parameters are already included
    var currentConfigNode = AllConfigurations.Last!;
    do
    {
      var config = currentConfigNode.Value;
      if (config.Parameters != null)
      {
        foreach (var parameter in config.Parameters)
        {
          if (parameterDictionary.ContainsKey(parameter.VariableName))
          {
            SilKitLogger?.Log(
              LogLevel.Info,
              $"Parameter '{parameter.VariableName}' was defined in multiple configurations.");
            parameterDictionary[parameter.VariableName] = parameter;
          }
          else
          {
            parameterDictionary.Add(parameter.VariableName, parameter);
          }
        }
      }

      currentConfigNode = currentConfigNode.Previous;
    } while (currentConfigNode != null);
  }

  private void UpdateVariablesDictionary(Dictionary<string, VariableConfiguration> variableConfigurationDictionary)
  {
    if (AllConfigurations == null)
    {
      throw new NullReferenceException("AllConfigurations was not initialized.");
    }

    if (AllConfigurations.Count == 0)
    {
      return;
    }

    // Last cannot be null here -> omit nullable
    // NB: The last handled entry of AllConfigurations is the configuration itself -> local parameters are already included
    var currentConfigNode = AllConfigurations.Last!;
    do
    {
      var config = currentConfigNode.Value;
      if (config.VariableMappings != null)
      {
        foreach (var variableConfiguration in config.VariableMappings)
        {
          if (variableConfigurationDictionary.ContainsKey(variableConfiguration.VariableName))
          {
            SilKitLogger?.Log(
              LogLevel.Info,
              $"Variable '{variableConfiguration.VariableName}' was defined in multiple configurations.");
            variableConfigurationDictionary[variableConfiguration.VariableName] = variableConfiguration;
          }
          else
          {
            variableConfigurationDictionary.Add(variableConfiguration.VariableName, variableConfiguration);
          }
        }
      }

      currentConfigNode = currentConfigNode.Previous;
    } while (currentConfigNode != null);
  }

  private string? GetFullPath(string path)
  {
    if (Path.IsPathFullyQualified(path))
    {
      if (File.Exists(path))
      {
        return path;
      }

      throw new FileNotFoundException($"The given file path '{path}' does not exist.");
    }

    if (ConfigurationPath == null)
    {
      throw new InvalidConfigurationException("ConfigurationPath not initialized correctly.");
    }

    var configPathDir = Path.GetDirectoryName(ConfigurationPath);
    if (!Directory.Exists(configPathDir))
    {
      throw new DirectoryNotFoundException("Failed to extract directory from ConfigurationPath.");
    }

    var combinedPath = Path.Combine(configPathDir, path);

    if (!File.Exists(combinedPath))
    {
      throw new InvalidConfigurationException(
        $"Failed to resolve IncludeDirectory '{path}' in configuration file '{ConfigurationPath}'.");
    }

    return combinedPath; // Path.Combine(configDir, Path.GetFileName(path));
  }

  private string Sha512CheckSum(string path)
  {
    using var sha512 = SHA512.Create();
    using var fileStream = File.OpenRead(path);

    return BitConverter.ToString(sha512.ComputeHash(fileStream));
  }
}
