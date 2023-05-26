// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Security.Cryptography;

namespace FmuImporter.Config;

public class Configuration : ConfigurationPublic
{
  public string? ConfigurationPath { get; set; }

  internal LinkedList<Configuration>? AllConfigurations { get; set; }

  private Dictionary<string, Parameter>? _resolvedParameters;

  internal Dictionary<string, Parameter> ResolvedParameters
  {
    get
    {
      if (_resolvedParameters == null)
      {
        _resolvedParameters = new Dictionary<string, Parameter>();
        UpdateParameterDictionary(ref _resolvedParameters);
      }

      return _resolvedParameters;
    }
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

    if (Include == null || ConfigurationPath == null)
    {
      return;
    }

    var fullPath = GetFullPath(ConfigurationPath);
    if (!File.Exists(fullPath))
    {
      throw new FileNotFoundException($"The configuration file '{ConfigurationPath}' was not initialized properly.");
    }

    var hashValue = Sha512CheckSum(fullPath);
    configHashes.Add(hashValue);
    var currentNode = AllConfigurations.AddFirst(this);

    do
    {
      var newConfigs = currentNode.ValueRef.MergeIncludes(ref configHashes, currentNode.Value);
      if (newConfigs != null && newConfigs.Count > 0)
      {
        foreach (var newConfig in newConfigs)
        {
          AllConfigurations.AddLast(newConfig);
        }
      }

      currentNode = currentNode.Next;
    } while (currentNode != null);
  }

  private List<Configuration>? MergeIncludes(ref HashSet<string> configHashes, Configuration currentConfiguration)
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
      configHashes.Add(hashValue);
      result.Add(config);
    }

    return result;
  }

  public Dictionary<string, Parameter> GetParameters()
  {
    return ResolvedParameters;
  }

  private void UpdateParameterDictionary(ref Dictionary<string, Parameter> parameterDictionary)
  {
    if (AllConfigurations == null || AllConfigurations.Count == 0)
    {
      throw new NullReferenceException("AllConfigurations was not initialized.");
    }

    // Last cannot be null here -> omit nullable
    // NB: The last entry of AllConfigurations is the configuration itself -> local parameters are already included
    var currentConfigNode = AllConfigurations.Last!;
    do
    {
      var config = currentConfigNode.Value;
      if (config.Parameters != null)
      {
        foreach (var parameter in config.Parameters)
        {
          if (parameterDictionary.ContainsKey(parameter.VarName))
          {
            // TODO print info to logger
            parameterDictionary[parameter.VarName] = parameter;
          }
          else
          {
            parameterDictionary.Add(parameter.VarName, parameter);
          }
        }
      }

      currentConfigNode = currentConfigNode.Previous;
    } while (currentConfigNode != null);
  }

  private string? GetFullPath(string path)
  {
    if (!File.Exists(path))
    {
      return null;
    }

    if (Path.IsPathFullyQualified(path))
    {
      return path;
    }

    if (ConfigurationPath == null)
    {
      throw new InvalidOperationException("ConfigurationPath not set");
    }

    var configDir = Path.GetDirectoryName(ConfigurationPath);
    if (configDir == null)
    {
      throw new InvalidOperationException("Failed to retrieve directory from current config file path");
    }

    return Path.Combine(configDir, path);
  }

  private string Sha512CheckSum(string path)
  {
    using var sha512 = SHA512.Create();
    using var fileStream = File.OpenRead(path);

    return BitConverter.ToString(sha512.ComputeHash(fileStream));
  }
}
