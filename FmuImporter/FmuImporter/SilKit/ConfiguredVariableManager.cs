using Fmi.Binding;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using SilKit.Services.PubSub;

namespace FmuImporter.SilKit;

public class ConfiguredVariableManager
{
  private IFmiBindingCommon Binding { get; }
  public ModelDescription ModelDescription { get; }

  private List<ConfiguredVariable> OutConfiguredVariables { get; }

  private Dictionary<uint/* refValue*/, ConfiguredVariable> InConfiguredVariables { get; }

  public ConfiguredVariableManager(IFmiBindingCommon binding, ModelDescription modelDescription)
  {
    Binding = binding;
    ModelDescription = modelDescription;

    OutConfiguredVariables = new List<ConfiguredVariable>();
    InConfiguredVariables = new Dictionary<uint, ConfiguredVariable>();
  }

  private void AddConfiguredVariable(ConfiguredVariable c)
  {
    if (c.FmuVariableDefinition == null)
    {
      // TODO warn or throw
      return;
    }

    switch (c.FmuVariableDefinition.Causality)
    {
      case Variable.Causalities.Output:
      case Variable.Causalities.Parameter:
      case Variable.Causalities.StructuralParameter:
        OutConfiguredVariables.Add(c);
        break;
      case Variable.Causalities.Input:
        InConfiguredVariables.Add(c.FmuVariableDefinition.ValueReference, c);
        break;
    }
  }

  public void AddPublisher(ConfiguredVariable c, IDataPublisher publisher)
  {
    if (c.FmuVariableDefinition == null)
    {
      // TODO return or throw
      return;
    }

    c.SilKitService = publisher;

    AddConfiguredVariable(c);
  }

  public void AddSubscriber(ConfiguredVariable c, IDataSubscriber subscriber)
  {
    if (c.FmuVariableDefinition == null)
    {
      // TODO return or throw
      return;
    }

    c.SilKitService = subscriber;

    AddConfiguredVariable(c);
  }

  public void PublishAllOutputData()
  {
    foreach (var configuredVariable in OutConfiguredVariables)
    {
      if (configuredVariable.SilKitService == null)
      {
        // TODO throw or warn
        continue;
      }
      var configuredVariableType = configuredVariable.FmuVariableDefinition!.VariableType;
      // TODO: Extend when introducing signal groups
      var valueRefArr = new [] { configuredVariable.FmuVariableDefinition!.ValueReference };
      if (configuredVariableType == typeof(float))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<float> result);

        // Apply regular unit transformation
        // SIL Kit value = ([FMU value] / factor) - offset
        foreach (var variable in result.ResultArray)
        {
          var mdVar = ModelDescription.Variables[variable.ValueReference];
          for (int i = 0; i < variable.Values.Length; i++)
          {
            var unit = mdVar.TypeDefinition?.Unit;
            if (unit != null)
            {
              var value = variable.Values[i];
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

          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(double))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<double> result);

        // Apply regular unit transformation
        // SIL Kit value = ([FMU value] / factor) - offset
        foreach (var variable in result.ResultArray)
        {
          var mdVar = ModelDescription.Variables[variable.ValueReference];
          for (int i = 0; i < variable.Values.Length; i++)
          {
            var unit = mdVar.TypeDefinition?.Unit;
            if (unit != null)
            {
              var value = variable.Values[i];
              // first reverse offset...
              if (unit.Offset.HasValue)
              {
                value -= unit.Offset.Value;
              }

              // ...then reverse factor
              if (unit.Factor.HasValue)
              {
                value /= unit.Factor.Value;
              }

              variable.Values[i] = value;
            }
          }

          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(sbyte))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<sbyte> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(byte))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<byte> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(short))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<short> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(ushort))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<ushort> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(int))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<int> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(uint))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<uint> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(long))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<long> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(ulong))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<ulong> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(bool))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<bool> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(string))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<string> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
      else if (configuredVariableType == typeof(IntPtr))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<IntPtr> result);
        foreach (var variable in result.ResultArray)
        {
          var byteArray = ApplyConfiguredTransformationAndEncode(variable, configuredVariable);
          ((IDataPublisher)configuredVariable.SilKitService).Publish(byteArray);
        }
      }
    }
  }

  public void SetValue(uint refValue, byte[] data)
  {
    var success = InConfiguredVariables.TryGetValue(refValue, out var configuredVariable);
    if (success && configuredVariable != null)
    {
      if (configuredVariable.Transformation != null)
      {
        if (configuredVariable.FmuVariableDefinition == null)
        {
          // TODO return or throw
          throw new Exception();
        }

        // Reverse type transformation
        data = TransformAndReencode(data, configuredVariable, ModelDescription.Variables[refValue]);
      }
      Binding.SetValue(refValue, data);
    }
  }

  private byte[] TransformAndReencode(byte[] inputData, ConfiguredVariable configuredVariable, Variable mdVar)
  {
    var transformation = configuredVariable.Transformation;
    if (transformation == null || transformation.Factor == null && transformation.Offset == null && string.IsNullOrEmpty(transformation.TypeDuringTransmission))
    {
      // the transformation block has no (useful) information -> return original data
      return inputData;
    }

    // TODO: Note in documentation that transformations on the receiver's side are a lot more expensive than on the sender's side
    var isScalar = !(mdVar.Dimensions != null && mdVar.Dimensions.Length > 0);
    var arraySize = 1; // scalar by default
    if (!isScalar)
    {
      arraySize = BitConverter.ToInt32(inputData, 0);
      inputData = inputData.Skip(4).ToArray();
    }

    var targetArray = new object[arraySize];

    if (string.IsNullOrEmpty(transformation.TypeDuringTransmission))
    {
      for (int i = 0; i < arraySize; i++)
      {
        // convert data to target type
        var offset = 0;
        targetArray[i] = Helpers.FromByteArray(inputData, mdVar.VariableType, offset, out offset);
      }
    }
    else
    {
      // convert byte array to transform type
      var offset = 0;
      for (int i = 0; i < arraySize; i++)
      {
        var transformType = Helpers.StringToType(transformation.TypeDuringTransmission.ToLowerInvariant());
        var transmissionData = Helpers.FromByteArray(inputData, transformType, offset, out offset);
        // re-encode data with variable type
        targetArray[i] = Convert.ChangeType(transmissionData, (Type)(mdVar.VariableType));
      }
    }

    // Apply factor and offset transform
    for (int i = 0; i < arraySize; i++)
    {
      var factor = transformation.Factor ?? 1;
      var offset = transformation.Offset ?? 0;
      Helpers.ApplyLinearTransformation(ref targetArray[i], factor, offset, mdVar.VariableType);
    }

    return Helpers.ToByteArray(targetArray, mdVar.VariableType);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<float>.Variable variable, ConfiguredVariable configuredVariable)
  {
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = (float)(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = (float)(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<double>.Variable variable, ConfiguredVariable configuredVariable)
  {
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = variable.Values[i] * configuredVariable.Transformation.Factor.Value;
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = variable.Values[i] + configuredVariable.Transformation.Offset.Value;
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<byte>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToByte(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToByte(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<UInt16>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToUInt16(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToUInt16(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<UInt32>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToUInt32(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToUInt32(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<UInt64>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToUInt64(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToUInt64(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<sbyte>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToSByte(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToSByte(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<Int16>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToInt16(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToInt16(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<Int32>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToInt32(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToInt32(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<Int64>.Variable variable, ConfiguredVariable configuredVariable)
  {
    // TODO (general) Convert.ToXXX behavior
    if (configuredVariable.Transformation != null)
    {
      // apply factor, then apply offset
      for (var i = 0; i < variable.Values.Length; i++)
      {
        if (configuredVariable.Transformation.Factor.HasValue)
        {
          variable.Values[i] = Convert.ToInt64(variable.Values[i] * configuredVariable.Transformation.Factor.Value);
        }

        if (configuredVariable.Transformation.Offset.HasValue)
        {
          variable.Values[i] = Convert.ToInt64(variable.Values[i] + configuredVariable.Transformation.Offset.Value);
        }

      }

      if (!string.IsNullOrEmpty(configuredVariable.Transformation.TypeDuringTransmission))
      {
        // cast type & encode
        switch (configuredVariable.Transformation.TypeDuringTransmission.ToLowerInvariant())
        {
          case "uint8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToByte), variable.IsScalar);
          case "uint16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt16), variable.IsScalar);
          case "uint32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt32), variable.IsScalar);
          case "uint64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToUInt64), variable.IsScalar);
          case "int8":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSByte), variable.IsScalar);
          case "int16":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt16), variable.IsScalar);
          case "int32":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt32), variable.IsScalar);
          case "int64":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToInt64), variable.IsScalar);
          case "float32":
          case "float":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToSingle), variable.IsScalar);
          case "float64":
          case "double":
            return SilKitManager.EncodeData(Array.ConvertAll(variable.Values, Convert.ToDouble), variable.IsScalar);
        }
      }
      else
      {
        // encode
        return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
      }
    }
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<bool>.Variable variable, ConfiguredVariable _)
  {
    // bools do not support transformation
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<string>.Variable variable, ConfiguredVariable _)
  {
    // strings do not support transformation
    return SilKitManager.EncodeData(variable.Values, variable.IsScalar);
  }

  private byte[] ApplyConfiguredTransformationAndEncode(ReturnVariable<IntPtr>.Variable variable, ConfiguredVariable _)
  {
    // binaries do not support transformation
    return SilKitManager.EncodeData(variable.Values, variable.ValueSizes, variable.IsScalar);
  }
}
