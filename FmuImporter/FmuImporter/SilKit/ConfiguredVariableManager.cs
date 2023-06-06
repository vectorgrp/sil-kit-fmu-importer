// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
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
            var unit = mdVar.TypeDefinition?.Unit;
            if (unit != null)
            {
              if (configuredVariableType == VariableTypes.Float32)
              {
                var value = (float)(variable.Values[i]);
                // first reverse offset...
                if (unit.Offset.HasValue)
                {
                  value = Convert.ToSingle(value - unit.Offset.Value);
                }

                // ...then reverse factor
                if (unit.Factor.HasValue)
                {
                  value = Convert.ToSingle(value / unit.Factor.Value);
                }

                variable.Values[i] = value;
              }
            }
          }
        }
      }

      foreach (var variable in result.ResultArray)
      {
        var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
        ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
      }
    }
  }

  public void SetValue(uint refValue, byte[] data)
  {
    var success = InputConfiguredVariables.TryGetValue(refValue, out var configuredVariable);
    if (success && configuredVariable != null)
    {
      if (configuredVariable.Transformation != null)
      {
        if (configuredVariable.FmuVariableDefinition == null)
        {
          throw new InvalidConfigurationException(
            $"{nameof(configuredVariable)} was not initialized correctly.",
            new NullReferenceException($"{nameof(configuredVariable.FmuVariableDefinition)} was null."));
        }

        // Reverse type transformation and apply linear transformation
        data = TransformAndReencode(data, configuredVariable, ModelDescription.Variables[refValue]);
      }

      Binding.SetValue(refValue, data);
    }
  }

  private byte[] TransformAndReencode(byte[] inputData, ConfiguredVariable configuredVariable, Variable mdVar)
  {
    var transformation = configuredVariable.Transformation;
    if (transformation == null ||
        (transformation.Factor == null &&
         transformation.Offset == null &&
         string.IsNullOrEmpty(transformation.TransmissionType)))
    {
      // the transformation block has no (useful) information -> return original data
      return inputData;
    }

    var isScalar = !(mdVar.Dimensions != null && mdVar.Dimensions.Length > 0);
    var arraySize = 1; // scalar by default
    if (!isScalar)
    {
      arraySize = BitConverter.ToInt32(inputData, 0);
      inputData = inputData.Skip(4).ToArray();
    }

    var targetArray = new object[arraySize];

    if (string.IsNullOrEmpty(transformation.TransmissionType))
    {
      for (var i = 0; i < arraySize; i++)
      {
        // convert data to target type
        var offset = 0;
        targetArray[i] = Helpers.FromByteArray(inputData, mdVar.VariableType, ref offset);
      }
    }
    else
    {
      // convert byte array to transform type
      var offset = 0;
      for (var i = 0; i < arraySize; i++)
      {
        var transformType = Helpers.StringToVariableType(transformation.TransmissionType.ToLowerInvariant());
        var transmissionData = Helpers.FromByteArray(inputData, transformType, ref offset);
        // re-encode data with variable type
        targetArray[i] = Convert.ChangeType(transmissionData, Helpers.VariableTypeToType(mdVar.VariableType));
      }
    }

    // Apply factor and offset transform
    for (var i = 0; i < arraySize; i++)
    {
      var factor = transformation.Factor ?? 1;
      var offset = transformation.Offset ?? 0;
      Helpers.ApplyLinearTransformation(ref targetArray[i], factor, offset, mdVar.VariableType);
    }

    return SerDes.Serialize(targetArray, isScalar, Helpers.VariableTypeToType(mdVar.VariableType));
  }

  private byte[] ApplyConfiguredTransformationAndEncode(
    ReturnVariable.Variable variable,
    ConfiguredVariable configuredVariable)
  {
    if (configuredVariable.Transformation != null)
    {
      // Apply factor and offset transform
      for (var i = 0; i < variable.Values.Length; i++)
      {
        var factor = configuredVariable.Transformation.Factor ?? 1;
        var offset = configuredVariable.Transformation.Offset ?? 0;
        Helpers.ApplyLinearTransformation(ref variable.Values[i], factor, offset, variable.Type);
      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TransmissionType))
      {
        return SerDes.Serialize(
          variable.Values,
          variable.IsScalar,
          Helpers.StringToType(configuredVariable.Transformation.TransmissionType),
          null);
      }

      // encode
      return SerDes.Serialize(variable.Values, variable.IsScalar, Helpers.VariableTypeToType(variable.Type), null);
    }

    return SerDes.Serialize(
      variable.Values,
      variable.IsScalar,
      Helpers.VariableTypeToType(variable.Type),
      Array.ConvertAll(variable.ValueSizes, e => (Int32)e));
  }
}
