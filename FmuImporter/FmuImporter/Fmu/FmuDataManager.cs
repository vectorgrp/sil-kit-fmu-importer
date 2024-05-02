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
  public Dictionary<long, ConfiguredStructure> ParameterConfiguredStructures { get; }
  private readonly Dictionary<string, ConfiguredStructure> _parameterConfiguredStructureByName;

  public Dictionary<long /* refValue*/, ConfiguredVariable> InputConfiguredVariables { get; }
  public Dictionary<long, ConfiguredStructure> InputConfiguredStructures { get; }
  private readonly Dictionary<string, ConfiguredStructure> _inputConfiguredStructureByName;

  public List<ConfiguredVariable> OutputConfiguredVariables { get; }
  public Dictionary<long, ConfiguredStructure> OutputConfiguredStructures { get; }
  private readonly Dictionary<string, ConfiguredStructure> _outputConfiguredStructureByName;

  public FmuDataManager(
    IFmiBindingCommon binding,
    ModelDescription modelDescription)
  {
    Binding = binding;
    ModelDescription = modelDescription;

    ParameterConfiguredVariables = new List<ConfiguredVariable>();
    ParameterConfiguredStructures = new Dictionary<long, ConfiguredStructure>();
    _parameterConfiguredStructureByName = new Dictionary<string, ConfiguredStructure>();

    InputConfiguredVariables = new Dictionary<long, ConfiguredVariable>();
    InputConfiguredStructures = new Dictionary<long, ConfiguredStructure>();
    _inputConfiguredStructureByName = new Dictionary<string, ConfiguredStructure>();

    OutputConfiguredVariables = new List<ConfiguredVariable>();
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
        return true;
    }

    var configStructFound = targetStructureInternal.TryGetValue(
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
      targetStructureInternal.Add(varStructName, configuredStructure);
      targetStructure.Add(configuredStructure.StructureId, configuredStructure);
    }

    configuredStructure!.AddMember(configuredVariable);

    return false;
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

  public List<Tuple<long, byte[]>> GetInitialVariableData()
  {
    var result = GetVariableParameterData();
    result.AddRange(GetVariableOutputData(true));
    return result;
  }

  public List<Tuple<long, byte[]>> GetVariableOutputData(bool initialKnownsOnly)
  {
    return GetVariableData(initialKnownsOnly, OutputConfiguredVariables);
  }

  public List<Tuple<long, byte[]>> GetStructureOutputData(bool initialKnownsOnly)
  {
    return GetStructureData(initialKnownsOnly, OutputConfiguredStructures);
  }

  public List<Tuple<long, byte[]>> GetVariableParameterData()
  {
    return GetVariableData(true, ParameterConfiguredVariables);
  }

  private List<Tuple<long, byte[]>> GetVariableData(
    bool initialKnownsOnly,
    List<ConfiguredVariable> configuredVariables)
  {
    var returnData = new List<Tuple<long, byte[]>>();

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
            Helpers.Helpers.ApplyLinearTransformationFmi(
              ref variable.Values[i],
              configuredVariable);
          }
        }
      }

      for (var i = 0; i < result.ResultArray.Length; i++)
      {
        var variable = result.ResultArray[i];
        Helpers.Helpers.ApplyLinearTransformationImporterConfig(ref variable.Values[i], configuredVariable);

        var dc = new DataConverter();
        var byteArray = dc.TransformToSilKitData(variable, configuredVariable);
        returnData.Add(Tuple.Create((long)configuredVariable.FmuVariableDefinition.ValueReference, byteArray));
      }
    }

    return returnData;
  }

  private List<Tuple<long, byte[]>> GetStructureData(
    bool initialKnownsOnly,
    Dictionary<long, ConfiguredStructure> configuredStructures)
  {
    // TODO
    //if (initialKnownsOnly &&
    //    ModelDescription.ModelStructure.InitialUnknowns.Contains(
    //      configuredVariable.FmuVariableDefinition.ValueReference))
    //{
    //  // skip initially unknown variables
    //  continue;
    //}

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
        var valueRefArr = new[]
        {
          structureMember.FmuVariableDefinition.ValueReference
        };
        Binding.GetValue(valueRefArr, out var result, configuredVariableType);

        // apply linear transformation to individual variables
        if (structureMember.FmuVariableDefinition.VariableType == VariableTypes.Float32 ||
            structureMember.FmuVariableDefinition.VariableType == VariableTypes.Float64)
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
      var byteArray = dc.TransformToSilKitData(memberData, configuredStructure);
      returnData.Add(Tuple.Create(configuredStructure.StructureId, byteArray));
    }

    return returnData;
  }

  public void SetData(uint refValue, byte[] data)
  {
    var fmuData = TransformSilKitToFmuData(refValue, data.ToList(), out var binSizes).ToArray();
    if (binSizes.Count == 0)
    {
      Binding.SetValue(refValue, fmuData);
    }
    else
    {
      Binding.SetValue(refValue, fmuData, binSizes.ToArray());
    }
  }

  public void SetData(Dictionary<long, byte[]> silKitDataMap)
  {
    foreach (var dataKvp in silKitDataMap)
    {
      if (dataKvp.Key <= uint.MaxValue)
      {
        // key is refValue
        SetData((uint)dataKvp.Key, dataKvp.Value);
      }
      else
      {
        SetStructure(dataKvp.Key, dataKvp.Value);
      }
    }
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

    var index = 0;
    foreach (var structureMember in configuredStructure!.StructureMembers)
    {
      if (structureMember == null)
      {
        throw new NullReferenceException(
          $"The currently transformed struct ({configuredStructure.Name}) has unmapped members.");
      }

      var refValue = structureMember.FmuVariableDefinition.ValueReference;

      if (binSizes[index].Count == 0)
      {
        Binding.SetValue(refValue, fmuData[index].ToArray());
      }
      else
      {
        Binding.SetValue(refValue, fmuData[index].ToArray(), binSizes[index].ToArray());
      }
    }
  }


  private List<List<byte>> TransformSilKitToStructuredFmuData(
    long refValue,
    byte[] silKitData,
    out List<List<int>> binSizes)
  {
    var success = InputConfiguredStructures.TryGetValue(refValue, out var configuredStructure);
    if (success && configuredStructure != null)
    {
      var dc = new DataConverter();
      return dc.TransformSilKitToStructuredFmuData(configuredStructure, silKitData.ToList(), out binSizes);
    }

    throw new ArgumentOutOfRangeException(
      $"Failed to transform received SIL Kit data. The target variable's reference value was {refValue}");
  }

  private List<byte> TransformSilKitToFmuData(long refValue, List<byte> silKitData, out List<int> binSizes)
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
