// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using Fmi.Supplements;
using FmuImporter.Config;
using FmuImporter.Models.CommDescription;
using FmuImporter.Models.Config;
using FmuImporter.Models.Exceptions;
using FmuImporter.Models.Helpers;

namespace FmuImporter.Fmu;

public class FmuDataManager
{
  private IFmiBindingCommon Binding { get; }
  private ModelDescription ModelDescription { get; }

  public List<ConfiguredVariable> ParameterConfiguredVariables { get; }
  public Dictionary<long, ConfiguredStructure> ParameterConfiguredStructures { get; }
  private readonly Dictionary<string, ConfiguredStructure> _parameterConfiguredStructureByName;

  public Dictionary<long /* refValue*/, ConfiguredVariable> InputConfiguredVariables { get; }
  public Dictionary<long, ConfiguredStructure> InputConfiguredStructures { get; }
  private readonly Dictionary<string, ConfiguredStructure> _inputConfiguredStructureByName;

  public Dictionary<VariableTypes, List<ConfiguredVariable>> OutputConfiguredVariables { get; }
  public Dictionary<long, ConfiguredStructure> OutputConfiguredStructures { get; }
  private readonly Dictionary<string, ConfiguredStructure> _outputConfiguredStructureByName;

  private readonly Action<LogSeverity, string> _logCallback;

  public FmuDataManager(
    IFmiBindingCommon binding,
    ModelDescription modelDescription,
    Action<LogSeverity, string> logCallback)
  {
    Binding = binding;
    ModelDescription = modelDescription;
    _logCallback = logCallback;

    ParameterConfiguredVariables = new List<ConfiguredVariable>();
    ParameterConfiguredStructures = new Dictionary<long, ConfiguredStructure>();
    _parameterConfiguredStructureByName = new Dictionary<string, ConfiguredStructure>();

    InputConfiguredVariables = new Dictionary<long, ConfiguredVariable>();
    InputConfiguredStructures = new Dictionary<long, ConfiguredStructure>();
    _inputConfiguredStructureByName = new Dictionary<string, ConfiguredStructure>();

    OutputConfiguredVariables = new Dictionary<VariableTypes, List<ConfiguredVariable>>();
    OutputConfiguredStructures = new Dictionary<long, ConfiguredStructure>();
    _outputConfiguredStructureByName = new Dictionary<string, ConfiguredStructure>();
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

    var useStructuredNamingConvention =
      (ModelDescription.VariableNamingConvention == ModelDescription.VariableNamingConventions.Structured) ||
      importerConfiguration.AlwaysUseStructuredNamingConvention;

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
          throw new NullReferenceException($"The retrieved configured variable was null. Exception thrown by the " +
            $"following value reference: {modelDescriptionVariable.ValueReference}");
        }
      }

      var configuredVariable = new ConfiguredVariable(variableConfiguration, modelDescriptionVariable);
      if (useStructuredNamingConvention)
      {
        var topic = configuredVariable.TopicName;
        configuredVariable.StructuredPath = StructuredVariableParser.Parse(topic, _logCallback);
      }
      else
      {
        configuredVariable.StructuredPath = new StructuredNameContainer(
          new List<string>()
          {
            configuredVariable.TopicName
          });
      }

      ProcessConfiguredVariable(configuredVariable, useStructuredNamingConvention, commInterface);
    }

    ValidateCommInterfaceMapping();
  }

  private void ProcessConfiguredVariable(
    ConfiguredVariable configuredVariable,
    bool useStructuredNamingConvention,
    CommunicationInterfaceInternal? commInterface)
  {
    if (useStructuredNamingConvention &&
        (commInterface != null) &&
        (configuredVariable.StructuredPath != null) &&
        (configuredVariable.StructuredPath.Path.Count > 1))
    {
      // use structure handling for data processing
      var structName = configuredVariable.StructuredPath.RootName;
      var structType = commInterface.Publishers?.FirstOrDefault(pub => pub.Name == structName)?.ResolvedType;
      if (structType == null)
      {
        structType = commInterface.Subscribers?.FirstOrDefault(sub => sub.Name == structName)?.ResolvedType;
      }

      if (structType!.Type == null)
      {
        AddOrCreateConfiguredStructureMember(
          configuredVariable,
          commInterface,
          structName,
          structType);
      }
    }
    else
    {
      // NB: If users provide a communication interface file, but no structured naming convention is used,
      // the Importer will currently !not! automatically fix missing transformations to optional types!
      AddConfiguredVariable(configuredVariable);
    }
  }

  private void AddConfiguredVariable(ConfiguredVariable c)
  {
    switch (c.FmuVariableDefinition.Causality)
    {
      case Variable.Causalities.Output:
        if (OutputConfiguredVariables.TryGetValue(c.FmuVariableDefinition.VariableType, out var refValue))
        {
          refValue.Add(c);
        }
        else
        {
          OutputConfiguredVariables[c.FmuVariableDefinition.VariableType] = [ c ];
        }
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

  private void AddOrCreateConfiguredStructureMember(
    ConfiguredVariable configuredVariable,
    CommunicationInterfaceInternal commInterface,
    string structName,
    OptionalType structType)
  {
    Dictionary<string, ConfiguredStructure> targetStructureInternal;
    Dictionary<long, ConfiguredStructure> targetStructure;
    switch (configuredVariable.FmuVariableDefinition.Causality)
    {
      case Variable.Causalities.Output:
        targetStructureInternal = _outputConfiguredStructureByName;
        targetStructure = OutputConfiguredStructures;
        break;
      case Variable.Causalities.Parameter:
      case Variable.Causalities.StructuralParameter:
        targetStructureInternal = _parameterConfiguredStructureByName;
        targetStructure = ParameterConfiguredStructures;
        break;
      case Variable.Causalities.Input:
        targetStructureInternal = _inputConfiguredStructureByName;
        targetStructure = InputConfiguredStructures;
        break;
      default:
        // handle as if it is a variable
        throw new InvalidCommunicationInterfaceException(
          $"The structure has a member variable with a causality other than Output, Parameter, " +
          $"StructuralParameter, or Input. " +
          $"Exception thrown by {configuredVariable.FmuVariableDefinition.Name} with the following value reference: " +
          $"{configuredVariable.FmuVariableDefinition.ValueReference}");
    }

    var configStructFound = targetStructureInternal.TryGetValue(
      structName,
      out var configuredStructure);
    if (!configStructFound)
    {
      var structDefinitionOfPubSubType =
        commInterface.StructDefinitions?.FirstOrDefault(sd => sd.Name == structType.CustomTypeName);
      if (structDefinitionOfPubSubType == null)
      {
        throw new InvalidConfigurationException($"A service has a type that is not defined! " +
          $"Exception thrown by {configuredVariable.FmuVariableDefinition.Name} with the following value reference: " +
          $"{configuredVariable.FmuVariableDefinition.ValueReference}");
      }

      var flattenedMembers = structDefinitionOfPubSubType.FlattenedMembers;
      configuredStructure = new ConfiguredStructure(
        structName,
        flattenedMembers.Select(fm => structName + "." + fm.QualifiedName),
        structType.IsOptional);
      targetStructureInternal.Add(structName, configuredStructure);
      targetStructure.Add(configuredStructure.StructureId, configuredStructure);
    }

    configuredStructure!.AddMember(configuredVariable);
  }

  private void ValidateCommInterfaceMapping()
  {
    var targetStructureDictionaryValues = new List<Dictionary<long, ConfiguredStructure>.ValueCollection>()
    {
      ParameterConfiguredStructures.Values,
      InputConfiguredStructures.Values,
      OutputConfiguredStructures.Values
    };
    foreach (var targetStructureDictionary in targetStructureDictionaryValues)
    {
      foreach (var configuredStructure in targetStructureDictionary)
      {
        foreach (var member in configuredStructure.StructureMembers)
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

  public List<Tuple<long, byte[]>> GetVariableOutputData()
  {
    return GetVariableData(OutputConfiguredVariables);
  }

  public List<Tuple<long, byte[]>> GetStructureOutputData()
  {
    return GetStructureData(OutputConfiguredStructures);
  }

  private List<Tuple<long, byte[]>> GetVariableData(
  Dictionary<VariableTypes, List<ConfiguredVariable>> configuredVariables)
  {
    var returnData = new List<Tuple<long, byte[]>>();

    foreach (var typeVars in configuredVariables)
    {
      var valueRefQuery = from configuredVariable in typeVars.Value
                          select configuredVariable.FmuVariableDefinition.ValueReference;

      Binding.GetValue(valueRefQuery.ToArray(), out var result, typeVars.Key);
      if ((typeVars.Key == VariableTypes.Float32) ||
          (typeVars.Key == VariableTypes.Float64))
      {
        // apply FMI unit transformation
        foreach (var variable in result.ResultArray)
        {
          var configuredVar = typeVars.Value.Find(x => x.FmuVariableDefinition.ValueReference == variable.ValueReference);
          if (configuredVar != null)
          {
            for (var i = 0; i < variable.Values.Length; i++)
            {
              // Apply unit transformation
              Helpers.Helpers.ApplyLinearTransformationFmi(ref variable.Values[i], configuredVar);
            }
          }
        }
      }

      foreach (var variable in result.ResultArray)
      {
        var configuredVar = typeVars.Value.Find(x => x.FmuVariableDefinition.ValueReference == variable.ValueReference);
        if (configuredVar != null)
        {
          for (var j = 0; j < variable.Values.Length; j++)
          {
            Helpers.Helpers.ApplyLinearTransformationImporterConfig(ref variable.Values[j], configuredVar);
          }

          var dc = new DataConverter();
          // NB: Currently, this method transforms the FMI variable as a whole, making it impossible to have optional members
          // Further, this prevents the data from being serialized as nested lists
          var byteArray = dc.TransformFmuToSilKitData(variable, configuredVar);
          returnData.Add(Tuple.Create((long)configuredVar.FmuVariableDefinition.ValueReference, byteArray));
        }
      }
    }

    return returnData;
  }

  private List<Tuple<long, byte[]>> GetStructureData(Dictionary<long, ConfiguredStructure> configuredStructures)
  {
    var returnData = new List<Tuple<long, byte[]>>();

    // retrieve individual structures
    foreach (var configuredStructure in configuredStructures.Values)
    {
      var memberData = new List<Fmi.Binding.Helper.ReturnVariable.Variable>();
      // retrieve data of individual variables
      foreach (var structureMember in configuredStructure.StructureMembers)
      {
        if (structureMember == null)
        {
          throw new NullReferenceException(
            $"The currently transformed struct ({configuredStructure.Name}) has unmapped members.");
        }

        var configuredVariableType = structureMember.FmuVariableDefinition!.VariableType;
        var valueRefArr = new[] { structureMember.FmuVariableDefinition.ValueReference };
        Binding.GetValue(valueRefArr, out var result, configuredVariableType);

        // apply linear transformation to individual variables
        if ((structureMember.FmuVariableDefinition.VariableType == VariableTypes.Float32) ||
            (structureMember.FmuVariableDefinition.VariableType == VariableTypes.Float64))
        {
          // apply FMI unit transformation
          foreach (var variable in result.ResultArray)
          {
            var mdVar = ModelDescription.Variables[variable.ValueReference];
            for (var i = 0; i < variable.Values.Length; i++)
            {
              // Apply unit transformation
              Helpers.Helpers.ApplyLinearTransformationFmi(
                ref variable.Values[i],
                structureMember);
            }
          }
        }

        for (var i = 0; i < result.ResultArray.Length; i++)
        {
          var variable = result.ResultArray[i];
          Helpers.Helpers.ApplyLinearTransformationImporterConfig(ref variable.Values[i], structureMember);
        }

        if (result.ResultArray.Length != 1)
        {
          throw new NotSupportedException("Currently, this method only supports to process one variable at a time.");
        }

        // NB: fix this if more than one value is retrieved at once
        memberData.Add(result.ResultArray[0]);
      }

      // serialize structure as a whole
      var dc = new DataConverter();
      var byteArray = dc.TransformFmuToSilKitData(memberData, configuredStructure);
      returnData.Add(Tuple.Create(configuredStructure.StructureId, byteArray));
    }

    return returnData;
  }


  public void SetData(Dictionary<long, byte[]> silKitDataMap)
  {
    foreach (var dataKvp in silKitDataMap)
    {
      if (dataKvp.Key <= uint.MaxValue)
      {
        // key is refValue
        SetVariable((uint)dataKvp.Key, dataKvp.Value);
      }
      else
      {
        SetStructure(dataKvp.Key, dataKvp.Value);
      }
    }
  }

  public void SetVariable(uint refValue, byte[] data)
  {
    var fmuData = TransformSilKitToFmuData(refValue, data.ToList(), out var binSizes);

    SetBinding(refValue, fmuData, binSizes);
  }

  public void SetStructure(long structureId, byte[] silKitData)
  {
    // retrieve the structural information of the received data
    var success = InputConfiguredStructures.TryGetValue(structureId, out var configuredStructure);
    if (!success)
    {
      // note: this should not be possible, as structureId is a local information
      throw new InvalidDataException("The received data belongs to an unknown structure ID.");
    }

    var fmuData = TransformSilKitToStructuredFmuData(structureId, silKitData, out var binSizes);
    if (fmuData == null)
    {
      // struct was optional and did not provide payload -> skip struct update
      return;
    }

    var index = 0;
    foreach (var structureMember in configuredStructure!.StructureMembers)
    {
      if (structureMember == null)
      {
        throw new NullReferenceException(
          $"The currently transformed struct ({configuredStructure.Name}) has unmapped members.");
      }

      var data = fmuData[index];
      var refValue = structureMember.FmuVariableDefinition.ValueReference;

      SetBinding(refValue, data, binSizes[index]);

      index++;
    }
  }

  public void SetBinding(uint refValue, byte[]? fmuData, List<int>? binSizes)
  {
    if (fmuData == null)
    {
      return;
    }

    if (binSizes?.Count > 0)
    {
      Binding.SetValue(refValue, fmuData, binSizes.ToArray());
    }
    else
    {
      Binding.SetValue(refValue, fmuData);
    }
  }

  private List<byte[]?>? TransformSilKitToStructuredFmuData(
    long refValue,
    byte[] silKitData,
    out List<List<int>> binSizes)
  {
    var success = InputConfiguredStructures.TryGetValue(refValue, out var configuredStructure);
    if (success && (configuredStructure != null))
    {
      var dc = new DataConverter();
      return dc.TransformSilKitToStructuredFmuData(configuredStructure, silKitData.ToList(), out binSizes);
    }

    throw new ArgumentOutOfRangeException(
      $"Failed to transform received SIL Kit data. The target variable's reference value was {refValue}");
  }

  private byte[]? TransformSilKitToFmuData(long refValue, List<byte> silKitData, out List<int> binSizes)
  {
    var success = InputConfiguredVariables.TryGetValue(refValue, out var configuredVariable);
    if (success && (configuredVariable != null))
    {
      var dc = new DataConverter();
      return dc.TransformSilKitToFmuData(configuredVariable, silKitData, out binSizes);
    }

    throw new ArgumentOutOfRangeException(
      $"Failed to transform received SIL Kit data. The target variable's reference value was {refValue}");
  }
}
