// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using Fmi.Supplements;
using FmuImporter.CommDescription;
using FmuImporter.Config;
using FmuImporter.Exceptions;

namespace FmuImporter.Fmu;

public class FmuDataManager
{
  private IFmiBindingCommon Binding { get; }
  private ModelDescription ModelDescription { get; }

  public List<ConfiguredVariable> ParameterConfiguredVariables { get; }
  public Dictionary<string, ConfiguredStructure> ParameterConfiguredStructures { get; }

  public Dictionary<uint /* refValue*/, ConfiguredVariable> InputConfiguredVariables { get; }
  public Dictionary<string, ConfiguredStructure> InputConfiguredStructures { get; }

  public List<ConfiguredVariable> OutputConfiguredVariables { get; }
  public Dictionary<string, ConfiguredStructure> OutputConfiguredStructures { get; }

  public FmuDataManager(
    IFmiBindingCommon binding,
    ModelDescription modelDescription)
  {
    Binding = binding;
    ModelDescription = modelDescription;

    ParameterConfiguredVariables = new List<ConfiguredVariable>();
    ParameterConfiguredStructures = new Dictionary<string, ConfiguredStructure>();

    InputConfiguredVariables = new Dictionary<uint, ConfiguredVariable>();
    InputConfiguredStructures = new Dictionary<string, ConfiguredStructure>();

    OutputConfiguredVariables = new List<ConfiguredVariable>();
    OutputConfiguredStructures = new Dictionary<string, ConfiguredStructure>();
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
  public void Initialize(Configuration importerConfiguration, CommunicationInterfaceInternal? commInterface)
  {
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
          is not (Variable.Causalities.Input
          or Variable.Causalities.Output
          or Variable.Causalities.Parameter
          or Variable.Causalities.StructuralParameter))
      {
        // ignore variables with other causalities, such as calculatedParameter or local
        continue;
      }

      var success =
        variableConfigurationsDictionary.TryGetValue(
          modelDescriptionVariable.ValueReference,
          out var variableConfiguration);
      if (!success)
      {
        // Only subscribe and publish unmapped variables if they are not ignored
        if (importerConfiguration.IgnoreUnmappedVariables)
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

      var useStructuredNamingConvention =
        ModelDescription.VariableNamingConvention == ModelDescription.VariableNamingConventions.Structured ||
        importerConfiguration.AlwaysUseStructuredNamingConvention;

      var configuredVariable = new ConfiguredVariable(variableConfiguration, modelDescriptionVariable);
      if (useStructuredNamingConvention)
      {
        var topic = configuredVariable.TopicName;
        // TODO check if this parser / structure is sufficient
        configuredVariable.StructuredPath = StructuredVariableParser.Parse(topic);
      }
      else
      {
        configuredVariable.StructuredPath = new StructuredNameContainer(
          new List<string>()
          {
            configuredVariable.TopicName
          });
      }

      // TODO there is more potential for simplification here!
      if (useStructuredNamingConvention && commInterface != null)
      {
        var handleAsVariable = AddConfiguredDictionaryEntry(
          configuredVariable,
          commInterface);
        if (handleAsVariable)
        {
          AddConfiguredVariable(configuredVariable);
        }
      }
      else
      {
        AddConfiguredVariable(configuredVariable);
      }
    }

    // TODO this needs to be fixed!
    //ValidateVcdlMapping();
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

  private bool AddConfiguredDictionaryEntry(
    ConfiguredVariable configuredVariable,
    CommunicationInterfaceInternal commInterface)
  {
    if (configuredVariable.StructuredPath == null || configuredVariable.StructuredPath.Path.Count == 1)
    {
      // scalar value -> skip structure handling
      return true;
    }

    var varStructName = configuredVariable.StructuredPath.InstanceName;
    var publisher = commInterface.Publishers?.FirstOrDefault(pub => pub.Name == varStructName);
    if (publisher == null)
    {
      // TODO better return value needed!
      // there is no publisher that corresponds to the defined struct name...?
      return true;
    }

    Dictionary<string, ConfiguredStructure> targetStructure;
    switch (configuredVariable.FmuVariableDefinition.Causality)
    {
      case Variable.Causalities.Output:
        targetStructure = OutputConfiguredStructures;
        break;
      case Variable.Causalities.Parameter:
      case Variable.Causalities.StructuralParameter:
        targetStructure = ParameterConfiguredStructures;
        break;
      case Variable.Causalities.Input:
        targetStructure = InputConfiguredStructures;
        break;
      default:
        // handle as if it is a variable
        return true;
    }

    var configStructFound = targetStructure.TryGetValue(
      varStructName,
      out var configuredStructure);
    if (!configStructFound)
    {
      var structDefinitionOfPubType = commInterface.StructDefinitions?.FirstOrDefault(sd => sd.Name == publisher.Type);
      if (structDefinitionOfPubType == null)
      {
        // TODO / FIXME enums must be checked as well
        throw new InvalidConfigurationException("A publisher has a type that is not defined!");
      }

      var flattenedMembers = structDefinitionOfPubType.FlattenedMembers;
      configuredStructure = new ConfiguredStructure(varStructName, flattenedMembers.Select(fm => fm.QualifiedName));
      targetStructure.Add(varStructName, configuredStructure);
    }

    configuredStructure!.AddMember(configuredVariable);

    return false;
  }

  private void ValidateCommInterfaceMapping()
  {
    var targetStructureDictionaryValues = new List<Dictionary<string, ConfiguredStructure>.ValueCollection>()
    {
      ParameterConfiguredStructures.Values,
      InputConfiguredStructures.Values,
      OutputConfiguredStructures.Values
    };
    foreach (var targetStructureDictionary in targetStructureDictionaryValues)
    {
      foreach (var configuredStructure in targetStructureDictionary)
      {
        foreach (var member in configuredStructure.SortedStructureMembers)
        {
          if (member == null)
          {
            throw new InvalidConfigurationException(
              "Not all members of the used communication interface structures are mapped correctly to FMU variables");
          }
        }
      }
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
      var valueRefArr = new[]
      {
        configuredVariable.FmuVariableDefinition.ValueReference
      };

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
