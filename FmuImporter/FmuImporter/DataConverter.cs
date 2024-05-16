// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Binding.Helper;
using FmuImporter.Config;
using SilKit.Supplements;

namespace FmuImporter;

public class DataConverter
{
  public byte[] TransformSilKitToFmuData(
    ConfiguredVariable configuredVariable,
    byte[] silKitData,
    out List<int> binSizes)
  {
    binSizes = new List<int>();
    return TransformSilKitToFmuData(silKitData, configuredVariable, ref binSizes);
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
      configuredVariable.FmuVariableDefinition!,
      ref binSizes);
  }

  private object TransformReceivedDataType(Deserializer deserializer, ConfiguredVariable configuredVariable)
  {
    var fmuType = Helpers.VariableTypeToType(configuredVariable.FmuVariableDefinition!.VariableType);

    // apply type conversion if required
    if (configuredVariable.ImporterVariableConfiguration.Transformation?.TransmissionType != null)
    {
      var receivedType =
        Helpers.StringToType(configuredVariable.ImporterVariableConfiguration.Transformation.TransmissionType);
      var deserializedData = deserializer.Deserialize(receivedType);
      // change data type to type expected by FMU
      return Convert.ChangeType(deserializedData, fmuType);
    }

    return deserializer.Deserialize(fmuType);
  }

  public byte[] TransformToSilKitData(
    ReturnVariable.Variable variable,
    ConfiguredVariable configuredVariable)
  {
    Type transmissionType;
    if (configuredVariable.ImporterVariableConfiguration.Transformation != null &&
        !string.IsNullOrEmpty(configuredVariable.ImporterVariableConfiguration.Transformation.TransmissionType))
    {
      transmissionType =
        Helpers.StringToType(configuredVariable.ImporterVariableConfiguration.Transformation.TransmissionType);
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
