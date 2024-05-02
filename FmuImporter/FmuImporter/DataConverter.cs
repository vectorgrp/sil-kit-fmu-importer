// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Binding.Helper;
using FmuImporter.Config;
using SilKit.Supplements;

namespace FmuImporter;

public class DataConverter
{
  public List<List<byte>> TransformSilKitToStructuredFmuData(
    ConfiguredStructure configuredStructure,
    List<byte> silKitData,
    out List<List<int>> binSizes)
  {
    binSizes = new List<List<int>>();
    return TransformSilKitToStructuredFmuData(silKitData, configuredStructure, ref binSizes);
  }

  private List<List<byte>> TransformSilKitToStructuredFmuData(
    List<byte> data,
    ConfiguredStructure configuredStructure,
    ref List<List<int>> binSizes)
  {
    var result = new List<List<byte>>();

    var deserializer = new Deserializer(data);
    deserializer.BeginStruct();

    foreach (var structureMember in configuredStructure.StructureMembers)
    {
      if (structureMember == null)
      {
        throw new NullReferenceException(
          $"The currently transformed struct ({configuredStructure.Name}) has unmapped members.");
      }

      var localBinSize = new List<int>();
      var localFmuData = new List<byte>();

      var isScalar = structureMember.FmuVariableDefinition.IsScalar;

      if (isScalar)
      {
        localFmuData.AddRange(ProcessDataEntity(ref deserializer, structureMember, ref localBinSize));
      }
      else
      {
        var byteArray = new List<byte>();
        var arrayLength = deserializer.BeginArray();
        for (var i = 0; i < arrayLength; i++)
        {
          var dataElement = ProcessDataEntity(ref deserializer, structureMember, ref localBinSize);
          byteArray.AddRange(dataElement);
        }

        deserializer.EndArray();
        localFmuData.AddRange(byteArray.ToArray());
      }

      binSizes.Add(localBinSize);
      result.Add(localFmuData);
    }

    deserializer.EndStruct();
    return result;
  }

  public List<byte> TransformSilKitToFmuData(
    ConfiguredVariable configuredVariable,
    List<byte> silKitData,
    out List<int> binSizes)
  {
    binSizes = new List<int>();
    return TransformSilKitToFmuData(silKitData, configuredVariable, ref binSizes);
  }

  private List<byte> TransformSilKitToFmuData(
    List<byte> data,
    ConfiguredVariable configuredVariable,
    ref List<int> binSizes)
  {
    var isScalar = configuredVariable.FmuVariableDefinition!.IsScalar;

    var deserializer = new Deserializer(data);
    if (isScalar)
    {
      return ProcessDataEntity(ref deserializer, configuredVariable, ref binSizes).ToList();
    }

    var bytes = new List<byte>();
    var arrayLength = deserializer.BeginArray();
    for (var i = 0; i < arrayLength; i++)
    {
      var dataElement = ProcessDataEntity(ref deserializer, configuredVariable, ref binSizes);
      bytes.AddRange(dataElement);
    }

    deserializer.EndArray();
    return bytes;
  }

  private byte[] ProcessDataEntity(
    ref Deserializer deserializer,
    ConfiguredVariable configuredVariable,
    ref List<int> binSizes)
  {
    var deserializedData = TransformReceivedDataType(deserializer, configuredVariable);
    Helpers.Helpers.ApplyLinearTransformationImporterConfig(ref deserializedData, configuredVariable);
    Helpers.Helpers.ApplyLinearTransformationFmi(ref deserializedData, configuredVariable);

    return Fmi.Supplements.Serializer.Serialize(
      deserializedData,
      configuredVariable.FmuVariableDefinition!,
      ref binSizes);
  }

  private object TransformReceivedDataType(Deserializer deserializer, ConfiguredVariable configuredVariable)
  {
    var fmuType = Helpers.Helpers.VariableTypeToType(configuredVariable.FmuVariableDefinition!.VariableType);

    // apply type conversion if required
    if (configuredVariable.ImporterVariableConfiguration.Transformation?.TransmissionType != null)
    {
      var receivedType =
        Helpers.Helpers.StringToType(configuredVariable.ImporterVariableConfiguration.Transformation.TransmissionType);
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
    if ((configuredVariable.ImporterVariableConfiguration.Transformation != null) &&
        !string.IsNullOrEmpty(configuredVariable.ImporterVariableConfiguration.Transformation.TransmissionType))
    {
      transmissionType =
        Helpers.Helpers.StringToType(configuredVariable.ImporterVariableConfiguration.Transformation.TransmissionType);
    }
    else
    {
      transmissionType = Helpers.Helpers.VariableTypeToType(variable.Type);
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

  public byte[] TransformToSilKitData(
    List<ReturnVariable.Variable> variables,
    ConfiguredStructure configuredStructure)
  {
    var serializer = new Serializer();
    serializer.BeginStruct();
    var index = 0;
    foreach (var structureMember in configuredStructure.StructureMembers)
    {
      var variable = variables[index];

      Type transmissionType;
      if ((structureMember!.ImporterVariableConfiguration.Transformation != null) &&
          !string.IsNullOrEmpty(structureMember.ImporterVariableConfiguration.Transformation.TransmissionType))
      {
        transmissionType =
          Helpers.Helpers.StringToType(structureMember.ImporterVariableConfiguration.Transformation.TransmissionType);
      }
      else
      {
        transmissionType = Helpers.Helpers.VariableTypeToType(variable.Type);
      }

      SerDes.Serialize(
        variable.Values,
        variable.IsScalar,
        transmissionType,
        Array.ConvertAll(variable.ValueSizes, e => (Int32)e),
        ref serializer);
      index++;
    }

    serializer.EndStruct();

    return serializer.ReleaseBuffer().ToArray();
  }
}
