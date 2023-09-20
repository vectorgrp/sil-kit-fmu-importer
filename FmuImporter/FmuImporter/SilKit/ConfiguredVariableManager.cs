// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.Binding.Helper;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using FmuImporter.Exceptions;
using SilKit.Services.PubSub;
using SilKit.Supplements;

namespace FmuImporter.SilKit;

public class ConfiguredVariableManager
{
  private IFmiBindingCommon Binding { get; }
  public ModelDescription ModelDescription { get; }

  private List<ConfiguredVariable> OutputConfiguredVariables { get; }
  private List<ConfiguredVariable> ParameterConfiguredVariables { get; }

  private Dictionary<uint /* refValue*/, ConfiguredVariable> InputConfiguredVariables { get; }

  public ConfiguredVariableManager(IFmiBindingCommon binding, ModelDescription modelDescription)
  {
    Binding = binding;
    ModelDescription = modelDescription;

    OutputConfiguredVariables = new List<ConfiguredVariable>();
    ParameterConfiguredVariables = new List<ConfiguredVariable>();
    InputConfiguredVariables = new Dictionary<uint, ConfiguredVariable>();
  }

  private void AddConfiguredVariable(ConfiguredVariable c)
  {
    if (c.FmuVariableDefinition == null)
    {
      throw new InvalidConfigurationException(
        $"{nameof(c)} was not initialized correctly.",
        new NullReferenceException($"{nameof(c.FmuVariableDefinition)} was null."));
    }

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

  public void AddPublisher(ConfiguredVariable c, IDataPublisher publisher)
  {
    if (c.FmuVariableDefinition == null)
    {
      throw new InvalidConfigurationException(
        $"{nameof(c)} was not initialized correctly.",
        new NullReferenceException($"{nameof(c.FmuVariableDefinition)} was null."));
    }

    c.SilKitService = publisher;

    AddConfiguredVariable(c);
  }

  public void AddSubscriber(ConfiguredVariable c, IDataSubscriber subscriber)
  {
    if (c.FmuVariableDefinition == null)
    {
      throw new InvalidConfigurationException(
        $"{nameof(c)} was not initialized correctly.",
        new NullReferenceException($"{nameof(c.FmuVariableDefinition)} was null."));
    }

    c.SilKitService = subscriber;

    AddConfiguredVariable(c);
  }

  public void PublishInitialData()
  {
    PublishParameterData();
    PublishOutputData(true);
  }

  public void PublishOutputData(bool initialKnownsOnly)
  {
    PublishData(initialKnownsOnly, OutputConfiguredVariables);
  }

  public void PublishParameterData()
  {
    PublishData(true, ParameterConfiguredVariables);
  }

  private void PublishData(bool initialKnownsOnly, List<ConfiguredVariable> configuredVariables)
  {
    foreach (var configuredVariable in configuredVariables)
    {
      if (configuredVariable.SilKitService == null || configuredVariable.FmuVariableDefinition == null)
      {
        throw new InvalidConfigurationException(
          $"{nameof(configuredVariable)} was not initialized correctly.",
          new NullReferenceException(
            $"{nameof(configuredVariable.SilKitService)} null? {(configuredVariable.SilKitService == null)}; " +
            $"{nameof(configuredVariable.FmuVariableDefinition)} null? {(configuredVariable.SilKitService == null)}."));
      }

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

        var byteArray = TransformToSilKitData(variable, configuredVariable);
        ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
      }
    }
  }

  public void SetValue(uint refValue, byte[] silKitData)
  {
    var success = InputConfiguredVariables.TryGetValue(refValue, out var configuredVariable);
    if (success && configuredVariable != null)
    {
      var binSizes = new List<int>();
      var fmuData = TransformSilKitToFmuData(silKitData, configuredVariable, ref binSizes);

      if (binSizes.Count == 0)
      {
        Binding.SetValue(refValue, fmuData);
      }
      else
      {
        Binding.SetValue(refValue, fmuData, binSizes.ToArray());
      }
    }
  }

  private byte[] TransformSilKitToFmuData(byte[] data, ConfiguredVariable configuredVariable, ref List<int> binSizes)
  {
    var isScalar = configuredVariable.FmuVariableDefinition!.IsScalar;

    var deserializer = new Deserializer(data.ToList());
    if (isScalar)
    {
      return ProcessDataEntity(ref deserializer, configuredVariable, ref binSizes);
    }

    var byteArray = new List<byte>();
    var arrayLength = deserializer.BeginArray();
    for (var i = 0; i < arrayLength; i++)
    {
      var dataElement = ProcessDataEntity(ref deserializer, configuredVariable, ref binSizes);
      byteArray.AddRange(dataElement);
    }

    deserializer.EndArray();
    return byteArray.ToArray();
  }

  private byte[] ProcessDataEntity(
    ref Deserializer deserializer,
    ConfiguredVariable configuredVariable,
    ref List<int> binSizes)
  {
    var deserializedData = TransformReceivedDataType(deserializer, configuredVariable);
    Helpers.ApplyLinearTransformationImporterConfig(ref deserializedData, configuredVariable);
    Helpers.ApplyLinearTransformationFmi(ref deserializedData, configuredVariable);

    return Fmi.Supplements.Serializer.Serialize(
      deserializedData,
      configuredVariable.FmuVariableDefinition!.VariableType,
      ref binSizes);
  }


  private object TransformReceivedDataType(Deserializer deserializer, ConfiguredVariable configuredVariable)
  {
    var fmuType = Helpers.VariableTypeToType(configuredVariable.FmuVariableDefinition!.VariableType);

    // apply type conversion if required
    if (configuredVariable.Transformation?.TransmissionType != null)
    {
      var receivedType = Helpers.StringToType(configuredVariable.Transformation.TransmissionType);
      var deserializedData = deserializer.Deserialize(receivedType);
      // change data type to type expected by FMU
      return Convert.ChangeType(deserializedData, fmuType);
    }

    return deserializer.Deserialize(fmuType);
  }

  private byte[] TransformToSilKitData(
    ReturnVariable.Variable variable,
    ConfiguredVariable configuredVariable)
  {
    Type transmissionType;
    if (configuredVariable.Transformation != null &&
        !string.IsNullOrEmpty(configuredVariable.Transformation.TransmissionType))
    {
      transmissionType = Helpers.StringToType(configuredVariable.Transformation.TransmissionType);
    }
    else
    {
      transmissionType = Helpers.VariableTypeToType(variable.Type);
    }

    var serializer = new Serializer();
    SerDes.Serialize(
      variable.Values,
      variable.IsScalar,
      transmissionType,
      Array.ConvertAll(variable.ValueSizes, e => (Int32)e),
      ref serializer);

    return serializer.ReleaseBuffer().ToArray();
  }
}
