// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using FmuImporter.Exceptions;

namespace FmuImporter.Fmu;

public class FmuDataManager
{
  private IFmiBindingCommon Binding { get; }
  private ModelDescription ModelDescription { get; }

  private List<ConfiguredVariable> OutputConfiguredVariables { get; }
  private List<ConfiguredVariable> ParameterConfiguredVariables { get; }

  private Dictionary<uint /* refValue*/, ConfiguredVariable> InputConfiguredVariables { get; }

  public FmuDataManager(
    IFmiBindingCommon binding,
    ModelDescription modelDescription)
  {
    Binding = binding;
    ModelDescription = modelDescription;

    OutputConfiguredVariables = new List<ConfiguredVariable>();
    ParameterConfiguredVariables = new List<ConfiguredVariable>();
    InputConfiguredVariables = new Dictionary<uint, ConfiguredVariable>();
  }

  /// <summary>
  ///   Initialize all variables that are part of the FMU.
  ///   If the importer configuration configures a variable, the importer configuration takes precedence.
  /// </summary>
  /// <param name="importerConfiguration">Configuration of the FMU Importer</param>
  /// <returns>A list of all variables that must be synchronized via SIL Kit.</returns>
  /// <exception cref="InvalidConfigurationException">
  ///   Thrown if one of the variables in the Importer configuration does not exist in the FMU's model description.
  /// </exception>
  /// <exception cref="NullReferenceException"></exception>
  public List<ConfiguredVariable> Initialize(Configuration importerConfiguration)
  {
    var relevantConfiguredVariables = new List<ConfiguredVariable>();

    var configuredVariableDictionary =
      new Dictionary<uint, ConfiguredVariable>(ModelDescription.Variables.Values.Count);

    var variableConfigurationsDictionary =
      new Dictionary<uint, VariableConfiguration>(ModelDescription.Variables.Values.Count);

    var variableMappings = importerConfiguration.GetVariables();

    for (var i = 0; i < variableMappings.Count; i++)
    {
      var variableConfiguration = variableMappings.ElementAt(i).Value;
      var success =
        ModelDescription.NameToValueReference.TryGetValue(variableConfiguration.VariableName, out var refValue);
      if (!success)
      {
        throw new InvalidConfigurationException(
          $"The configured variable '{variableConfiguration.VariableName}' cannot be found in the model description.");
      }

      variableConfigurationsDictionary.Add(refValue, variableConfiguration);
    }

    foreach (var modelDescriptionVariable in ModelDescription.Variables.Values)
    {
      if (modelDescriptionVariable.Causality
          is Variable.Causalities.Input
          or Variable.Causalities.Output
          or Variable.Causalities.Parameter
          or Variable.Causalities.StructuralParameter)
      {
        var success =
          variableConfigurationsDictionary.TryGetValue(
            modelDescriptionVariable.ValueReference,
            out var variableConfiguration);
        if (!success)
        {
          // Only subscribe and publish unmapped variables if they are not ignored
          if (importerConfiguration.IgnoreUnmappedVariables.GetValueOrDefault(false))
          {
            continue;
          }

          // initialize a default configured variable
          variableConfiguration = new VariableConfiguration(
            modelDescriptionVariable.Name,
            modelDescriptionVariable.Name);
        }
        else
        {
          if (variableConfiguration == null)
          {
            throw new NullReferenceException("The retrieved configured variable was null.");
          }
        }

        if (string.IsNullOrEmpty(variableConfiguration.TopicName))
        {
          variableConfiguration.TopicName = modelDescriptionVariable.Name;
        }

        var configuredVariable = new ConfiguredVariable(variableConfiguration, modelDescriptionVariable);
        configuredVariableDictionary.Add(modelDescriptionVariable.ValueReference, configuredVariable);

        relevantConfiguredVariables.Add(configuredVariable);
        if (configuredVariable.FmuVariableDefinition.Causality
            is Variable.Causalities.Input
            or Variable.Causalities.Output
            or Variable.Causalities.Parameter
            or Variable.Causalities.StructuralParameter
           )
        {
          AddConfiguredVariable(configuredVariable);
        }
        // ignore variables with other causalities, such as calculatedParameter or local
      }
    }

    return relevantConfiguredVariables;
  }

  private void AddConfiguredVariable(ConfiguredVariable c)
  {
    switch (c.FmuVariableDefinition.Causality)
    {
      case Variable.Causalities.Output:
        OutputConfiguredVariables.Add(c);
        break;
      case Variable.Causalities.Parameter:
      case Variable.Causalities.StructuralParameter:
        ParameterConfiguredVariables.Add(c);
        break;
      case Variable.Causalities.Input:
        InputConfiguredVariables.Add(c.FmuVariableDefinition.ValueReference, c);
        break;
    }
  }

  public List<Tuple<uint, byte[]>> GetInitialData()
  {
    var result = GetParameterData();
    result.AddRange(GetOutputData(true));
    return result;
  }

  public List<Tuple<uint, byte[]>> GetOutputData(bool initialKnownsOnly)
  {
    return GetData(initialKnownsOnly, OutputConfiguredVariables);
  }

  public List<Tuple<uint, byte[]>> GetParameterData()
  {
    return GetData(true, ParameterConfiguredVariables);
  }

  private List<Tuple<uint, byte[]>> GetData(bool initialKnownsOnly, List<ConfiguredVariable> configuredVariables)
  {
    var returnData = new List<Tuple<uint, byte[]>>();

    foreach (var configuredVariable in configuredVariables)
    {
      if (initialKnownsOnly &&
          ModelDescription.ModelStructure.InitialUnknowns.Contains(
            configuredVariable.FmuVariableDefinition.ValueReference))
      {
        // skip initially unknown variables
        continue;
      }

      var configuredVariableType = configuredVariable.FmuVariableDefinition!.VariableType;
      // TODO: Extend when introducing signal groups
      var valueRefArr = new[] { configuredVariable.FmuVariableDefinition.ValueReference };

      Binding.GetValue(valueRefArr, out var result, configuredVariableType);
      if (configuredVariable.FmuVariableDefinition.VariableType == VariableTypes.Float32 ||
          configuredVariable.FmuVariableDefinition.VariableType == VariableTypes.Float64)
      {
        // apply FMI unit transformation
        foreach (var variable in result.ResultArray)
        {
          var mdVar = ModelDescription.Variables[variable.ValueReference];
          for (var i = 0; i < variable.Values.Length; i++)
          {
            // Apply unit transformation
            Helpers.ApplyLinearTransformationFmi(
              ref variable.Values[i],
              configuredVariable);
          }
        }
      }

      for (var i = 0; i < result.ResultArray.Length; i++)
      {
        var variable = result.ResultArray[i];
        Helpers.ApplyLinearTransformationImporterConfig(ref variable.Values[i], configuredVariable);

        var dc = new DataConverter();
        var byteArray = dc.TransformToSilKitData(variable, configuredVariable);
        returnData.Add(Tuple.Create(configuredVariable.FmuVariableDefinition.ValueReference, byteArray));
      }
    }

    return returnData;
  }

  public void SetData(uint refValue, byte[] data)
  {
    var fmuData = TransformSilKitToFmuData(refValue, data, out var binSizes);
    if (binSizes.Count == 0)
    {
      Binding.SetValue(refValue, fmuData);
    }
    else
    {
      Binding.SetValue(refValue, fmuData, binSizes.ToArray());
    }
  }

  public void SetData(Dictionary<uint, byte[]> silKitDataMap)
  {
    foreach (var dataKvp in silKitDataMap)
    {
      SetData(dataKvp.Key, dataKvp.Value);
    }
  }


  private byte[] TransformSilKitToFmuData(uint refValue, byte[] silKitData, out List<int> binSizes)
  {
    var success = InputConfiguredVariables.TryGetValue(refValue, out var configuredVariable);
    if (success && configuredVariable != null)
    {
      var dc = new DataConverter();
      return dc.TransformSilKitToFmuData(configuredVariable, silKitData, out binSizes);
    }

    throw new ArgumentOutOfRangeException(
      $"Failed to transform received SIL Kit data. The target variable's reference value was {refValue}");
  }
}
