// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using Fmi;
using Fmi.Binding.Helper;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using FmuImporter.Models.Exceptions;
using FmuImporter.Models.Helpers;
using SilKit.Services;
using SilKit.Services.Can;
using SilKit.Services.Logger;
using SilKit.Supplements;

namespace FmuImporter;

public class DataConverter
{
#region SIL Kit -> FMU

#region SIL Kit -> FMU (structured)

  public List<byte[]?>? TransformSilKitToStructuredFmuData(
    ConfiguredStructure configuredStructure,
    List<byte> silKitData,
    out List<List<int>> binSizes,
    bool useClockPubSubElements)
  {
    binSizes = new List<List<int>>();
    var result = new List<byte[]?>();

    var deserializer = new Deserializer(silKitData);

    if (configuredStructure.IsOptional && !deserializer.BeginOptional())
    {
      // structure is optional and does not provide payload -> return null
      return null;
    }

    deserializer.BeginStruct();

    foreach (var structureMember in configuredStructure.StructureMembers)
    {
      if (structureMember == null)
      {
        throw new NullReferenceException(
          $"The currently transformed struct ({configuredStructure.Name}) has unmapped members.");
      }

      if ((structureMember.FmuVariableDefinition.VariableType == VariableTypes.TriggeredClock) && !useClockPubSubElements)
      {
        continue;
      }

      var localBinSize = new List<int>();
      // direct inner call skips deserializer initialization (because it's already available)
      var localFmuData = TransformSilKitToFmuData(deserializer, structureMember, localBinSize);
      result.Add(localFmuData);
      binSizes.Add(localBinSize);
    }

    deserializer.EndStruct();
    return result;
  }

#endregion SIL Kit -> FMU (structured)

#region SIL Kit -> FMU (plain)

  public byte[]? TransformSilKitToFmuData(
    ConfiguredVariable configuredVariable,
    List<byte> silKitData,
    out List<int> binSizes,
    bool useClockPubSubElements)
  {
    binSizes = new List<int>();
    var deserializer = new Deserializer(silKitData);

    // Extra check since the clock variables were not added to configuredVariable
    if ((configuredVariable.FmuVariableDefinition.VariableType == VariableTypes.TriggeredClock) && !useClockPubSubElements)
    {
      return null;
    }

    return TransformSilKitToFmuData(deserializer, configuredVariable, binSizes);
  }

  private byte[]? TransformSilKitToFmuData(
    Deserializer deserializer,
    ConfiguredVariable configuredVariable,
    List<int> binSizes)
  {
    var deserializedEntites = ProcessDataEntity(deserializer, configuredVariable, binSizes);
    if (deserializedEntites == null || deserializedEntites.Count == 0)
    {
      return null;
    }

    var buffer = new byte[deserializedEntites.Sum(byteArray => byteArray.Length)];
    using var stream = new MemoryStream(buffer);
    foreach (var bytes in deserializedEntites)
    {
      stream.Write(bytes, 0, bytes.Length);
    }

    return buffer;
  }

  private List<byte[]>? ProcessDataEntity(
    Deserializer deserializer,
    ConfiguredVariable configuredVariable,
    List<int> binSizes)
  {
    var bytes = new List<byte[]>();
    var deserializedObjects = DeserializeFromSilKit(deserializer, configuredVariable);
    if (deserializedObjects == null)
    {
      return null;
    }

    for (var i = 0; i < deserializedObjects.Count; i++)
    {
      var deserializedObject = deserializedObjects[i];
      if (deserializedObject != null)
      {
        Helpers.Helpers.ApplyLinearTransformationImporterConfig(ref deserializedObject, configuredVariable);
        Helpers.Helpers.ApplyLinearTransformationFmi(ref deserializedObject, configuredVariable);
        var fmiData = Fmi.Supplements.Serializer.Serialize(
          deserializedObject,
          configuredVariable.FmuVariableDefinition!,
          binSizes);
        bytes.Add(fmiData);
      }
      else
      {
        // NB: currently, lists must be fully populated
        throw new InvalidOperationException(
          $"The list of deserialized objects for variable '{configuredVariable.TopicName}' with value reference " +
          $"{configuredVariable.FmuVariableDefinition.ValueReference}, is not fully populated.");
      }
    }

    return bytes;
  }

  public byte[] SilKitCanFrameToLsCanTransmitOperation(CanFrame silkitCanFrame)
  {
    ushort dataSize = (ushort)silkitCanFrame.data.size;

    CanTransmitOperation canTransmitOp = new CanTransmitOperation(dataSize);
    canTransmitOp.SetOPCode((int)CanOperations.CAN_Transmit);
    canTransmitOp.SetLength((uint)16 + dataSize); // 16 : header fields (without data)
    canTransmitOp.SetID(silkitCanFrame.id);
    canTransmitOp.SetIde((silkitCanFrame.flags & (uint)SilKitCanFrameFlag.Ide) == 0 ? 0 : 1);
    canTransmitOp.SetRtr((silkitCanFrame.flags & (uint)SilKitCanFrameFlag.Rtr) == 0 ? 0 : 1);
    canTransmitOp.SetData(silkitCanFrame.data.data);

    return canTransmitOp.GetBytes();
  }

#endregion SIL Kit -> FMU (plain)

  private List<object?>? DeserializeFromSilKit(
    Deserializer deserializer, ConfiguredVariable configuredVariable)
  {
    var targetType = configuredVariable.FmuVariableDefinition!.VariableType;
    var att = configuredVariable.ImporterVariableConfiguration.Transformation?.ResolvedTransmissionType;
    if (att != null)
    {
      if (configuredVariable.FmuVariableDefinition.VcdlStructMaxSize is not null)
      {
        return new List<object?>
        {
          deserializer.DeserializeRaw((int)configuredVariable.FmuVariableDefinition.VcdlStructMaxSize)
        };
      }

      return DeserializeFromSilKit(deserializer, att, targetType);
    }

    var type = Helpers.Helpers.VariableTypeToType(targetType);
    // regular deserialization
    if (configuredVariable.FmuVariableDefinition.IsScalar)
    {
      if (configuredVariable.FmuVariableDefinition.VcdlStructMaxSize is not null)
      {
        return new List<object?>
        {
          deserializer.DeserializeRaw((int)configuredVariable.FmuVariableDefinition.VcdlStructMaxSize)
        };
      }

      return new List<object?>
      {
        DeserializeFromSilKit(deserializer, type, type)
      };
    }

    // multidimensional array
    var dimensions = configuredVariable.FmuVariableDefinition.Dimensions;
    if (dimensions != null && dimensions.Length > 1)
    {
      var result = new List<object?>();
      DeserializeMultidimensionalArray(
        deserializer, type, dimensions, 0, result,
        configuredVariable.FmuVariableDefinition.VcdlStructMaxSize);
      return result;
    }

    // 1D array
    var flatResult = new List<object?>();
    var objectCount = deserializer.BeginArray();
    for (var i = 0; i < objectCount; i++)
    {
      flatResult.Add(configuredVariable.FmuVariableDefinition.VcdlStructMaxSize is not null
          ? deserializer.DeserializeRaw((int)configuredVariable.FmuVariableDefinition.VcdlStructMaxSize)
          : DeserializeFromSilKit(deserializer, type, type));
    }

    deserializer.EndArray();

    return flatResult;
  }

  private List<object?>? DeserializeFromSilKit(
    Deserializer deserializer,
    OptionalType att,
    VariableTypes targetVariableType)
  {
    if (!string.IsNullOrEmpty(att.CustomTypeName))
    {
      throw new NotSupportedException("The direct deserialization of custom data types is not supported.");
    }

    if (att.IsOptional && !deserializer.BeginOptional())
    {
      deserializer.EndOptional();
      // Optional data without content -> return null
      return null;
    }

    var targetType = Helpers.Helpers.VariableTypeToType(targetVariableType);

    if (att.IsList == true)
    {
      if (att.InnerType == null)
      {
        throw new InvalidCommunicationInterfaceException(
          $"The parsed transmission type (IsOptional={att.IsOptional}, IsList={att.IsList}, " +
          $"Type={att.Type?.Name ?? "null"}, CustomTypeName={att.CustomTypeName ?? "null"}) " +
          $"with target variable type '{targetVariableType}' contained a list without an inner type. This is not allowed.");
      }

      if (!string.IsNullOrEmpty(att.InnerType.CustomTypeName))
      {
        throw new NotSupportedException("The direct deserialization of lists of custom data types is not supported.");
      }

      var l = new List<object?>();
      var entryCount = deserializer.BeginArray();

      if (att.InnerType.IsList == true)
      {
        for (var i = 0; i < entryCount; i++)
        {
          var result = DeserializeFromSilKit(deserializer, att.InnerType, targetVariableType);
          if (result != null)
          {
            l.AddRange(result);
          }
        }
      }
      else
      {
        for (var i = 0; i < entryCount; i++)
        {
          l.Add(DeserializeFromSilKit(deserializer, att.InnerType.Type, targetType));
        }
      }

      deserializer.EndArray();
      return l;
    }
    else
    {
      // deserialize scalar
      var scalar = DeserializeFromSilKit(deserializer, att.Type, targetType);
      deserializer.EndOptional();
      return new List<object?>
      {
        scalar
      };
    }
  }

  // Deserialization without transformation
  private object? DeserializeFromSilKit(
    Deserializer deserializer, Type? t, Type targetType)
  {
    if (t == null)
    {
      throw new InvalidCommunicationInterfaceException("Deserialized data must be a built-in data type.");
    }

    var deserializedData = deserializer.Deserialize(t);

    if (targetType == typeof(Enum))
    {
      return Convert.ChangeType(
        deserializedData,
        typeof(long));
    }
    else
    {
      return Convert.ChangeType(
        deserializedData,
        targetType);
    }
  }

  // Recursively deserialize nested SIL Kit arrays
  private void DeserializeMultidimensionalArray(
    Deserializer deserializer,
    Type elementType,
    ulong[] dimensions,
    int currentDimIndex,
    List<object?> flatResult,
    int? vcdlStructMaxSize)
  {
    var arraySize = deserializer.BeginArray();

    if (currentDimIndex == dimensions.Length - 1)
    {
      // Innermost dimension: deserialize the actual elements
      for (var i = 0; i < arraySize; i++)
      {
        flatResult.Add(vcdlStructMaxSize is not null
            ? deserializer.DeserializeRaw((int)vcdlStructMaxSize)
            : DeserializeFromSilKit(deserializer, elementType, elementType));
      }
    }
    else
    {
      // Outer dimension: recurse into nested arrays
      for (var i = 0; i < arraySize; i++)
      {
        DeserializeMultidimensionalArray(
          deserializer, elementType, dimensions, currentDimIndex + 1, flatResult, vcdlStructMaxSize);
      }
    }

    deserializer.EndArray();
  }

#endregion SIL Kit -> FMU

#region FMU -> SIL Kit

#region FMU -> SIL Kit (structured)

  public byte[] TransformFmuToSilKitData(
    List<ReturnVariable.Variable> variables,
    ConfiguredStructure configuredStructure)
  {
    var serializer = new Serializer();
    if (configuredStructure.IsOptional)
    {
      serializer.BeginOptional(true);
    }

    serializer.BeginStruct();
    var index = 0;
    foreach (var structureMember in configuredStructure.StructureMembers)
    {
      var variable = variables[index];

      // add member to serializer
      TransformFmuToSilKitData(variable, structureMember!, serializer);
      index++;
    }

    serializer.EndStruct();
    if (configuredStructure.IsOptional)
    {
      serializer.EndOptional();
    }

    return serializer.ReleaseBuffer().ToArray();
  }

#endregion FMU -> SIL Kit (structured)

#region FMU -> SIL Kit (plain)

  public byte[] TransformFmuToSilKitData(
    ReturnVariable.Variable variable,
    ConfiguredVariable configuredVariable)
  {
    var serializer = new Serializer();
    TransformFmuToSilKitData(variable, configuredVariable, serializer);
    return serializer.ReleaseBuffer().ToArray();
  }

  private void TransformFmuToSilKitData(
    ReturnVariable.Variable variable,
    ConfiguredVariable configuredVariable,
    Serializer serializer)
  {
    if ((configuredVariable.ImporterVariableConfiguration.Transformation != null) &&
        (configuredVariable.ImporterVariableConfiguration.Transformation.ResolvedTransmissionType != null))
    {
      SerializeToSilKit(
        variable.Values,
        configuredVariable.ImporterVariableConfiguration.Transformation.ResolvedTransmissionType,
        Array.ConvertAll(variable.ValueSizes, e => (Int32)e),
        variable.Dimensions,
        serializer);
    }
    else
    {
      var sourceType = Helpers.Helpers.VariableTypeToType(variable.Type);
      var valueSizes = Array.ConvertAll(variable.ValueSizes, e => (Int32)e);
      var isVcdlStruct = configuredVariable.FmuVariableDefinition.VcdlStructMaxSize is not null;

      if (sourceType != typeof(IntPtr) && sourceType != typeof(byte[]))
      {
        SerializeToSilKit(variable.Values, variable.IsScalar, sourceType, serializer, variable.Dimensions);
      }
      else
      {
        SerializeToSilKit(
          variable.Values,
          variable.IsScalar,
          isVcdlStruct,
          sourceType,
          valueSizes,
          serializer);
      }
    }
  }

  private void SerializeToSilKit(
    object[] objectArray, 
    bool isScalar, 
    Type sourceType, 
    Serializer serializer, 
    ulong[]? dimensions)
  {
    // multidimensional array, nested BeginArray - EndArray
    if (!isScalar && dimensions != null && dimensions.Length > 1)
    {
      SerializeMultidimensionalArray(objectArray, sourceType, dimensions, 0, 0, serializer);
      return;
    }

    if (!isScalar)
    {
      serializer.BeginArray(objectArray.Length);
    }

    SerializeToSilKit(objectArray, sourceType, serializer);
    if (!isScalar)
    {
      serializer.EndArray();
    }
  }

  private int SerializeMultidimensionalArray(
    object[] flatArray,
    Type sourceType,
    ulong[] dimensions,
    int currentDimIndex,
    int flatOffset,
    Serializer serializer)
  {
    var dimSize = (int)dimensions[currentDimIndex];
    serializer.BeginArray(dimSize);

    if (currentDimIndex == dimensions.Length - 1)
    {
      // innermost dimension: serialize actual elements
      var slice = new object[dimSize];
      Array.Copy(flatArray, flatOffset, slice, 0, dimSize);
      SerializeToSilKit(slice, sourceType, serializer);
      flatOffset += dimSize;
    }
    else
    {
      // outer dimension: recurse
      for (var i = 0; i < dimSize; i++)
      {
        flatOffset = SerializeMultidimensionalArray(
          flatArray, sourceType, dimensions, currentDimIndex + 1, flatOffset, serializer);
      }
    }

    serializer.EndArray();
    return flatOffset;
  }

  // Serialize multidimensional array with optional type handling
  private int SerializeMultidimensionalArrayWithOptional(
    object[] flatArray,
    OptionalType targetType,
    int[]? valueSizes,
    ulong[] dimensions,
    int currentDimIndex,
    int flatOffset,
    Serializer serializer)
  {
    var dimSize = (int)dimensions[currentDimIndex];
    serializer.BeginArray(dimSize);

    if (currentDimIndex == dimensions.Length - 1)
    {
      // innermost dimension: serialize actual elements
      var elementType = targetType;
      while (elementType.IsList == true && elementType.InnerType != null)
      {
        elementType = elementType.InnerType;
      }

      for (var i = 0; i < dimSize; i++)
      {
        var entry = new object[] { flatArray[flatOffset + i] };
        SerializeToSilKit(entry, elementType, valueSizes, null, serializer);
      }
      flatOffset += dimSize;
    }
    else
    {
      // outer dimension: recurse
      var innerType = targetType.InnerType!;
      for (var i = 0; i < dimSize; i++)
      {
        flatOffset = SerializeMultidimensionalArrayWithOptional(
          flatArray, innerType, valueSizes, dimensions, currentDimIndex + 1, flatOffset, serializer);
      }
    }

    serializer.EndArray();
    return flatOffset;
  }

  private static ushort CalculateDlc(ushort dataFieldSize)
  {
    return (dataFieldSize <= 8) ? dataFieldSize :
           (dataFieldSize <= 12) ? (ushort)9 :
           (dataFieldSize <= 16) ? (ushort)10 :
           (dataFieldSize <= 20) ? (ushort)11 :
           (dataFieldSize <= 24) ? (ushort)12 :
           (dataFieldSize <= 32) ? (ushort)13 :
           (dataFieldSize <= 48) ? (ushort)14 :
           (dataFieldSize <= 64) ? (ushort)15 :
                                   (ushort)0xFFFF;
  }

  public CanFrame LsCanTransmitOperationToSilKitCanFrame(byte[] LSCanFrame, ILogger Logger)
  {
    int size = Marshal.SizeOf(typeof(CanTransmitOperation));
    IntPtr ptr = Marshal.AllocHGlobal(size);
    Marshal.Copy(LSCanFrame, 0, ptr, LSCanFrame.Length);
    var ptrToStruct = Marshal.PtrToStructure(ptr, typeof(CanTransmitOperation));
    Marshal.FreeHGlobal(ptr);

    if (ptrToStruct == null)
    {
      Logger.Log(LogLevel.Error, $"Error while casting CAN transmit operation to SIL Kit CAN frame. Retreived bytes:" +
        $"{LSCanFrame}");
      return new CanFrame();
    }

    CanTransmitOperation canTransmitOp = (CanTransmitOperation)ptrToStruct;

    CanFrame silkitCanFrame = new CanFrame();

    silkitCanFrame.id = canTransmitOp.GetID();
    if (canTransmitOp.GetIde() == 1)
    {
      silkitCanFrame.flags |= (UInt32)SilKitCanFrameFlag.Ide;
    }
    if (canTransmitOp.GetRtr() == 1)
    {
      silkitCanFrame.flags |= (UInt32)SilKitCanFrameFlag.Rtr;
    }
    var dataLength = canTransmitOp.GetDataLength();
    silkitCanFrame.dlc = CalculateDlc(dataLength);
    if ( silkitCanFrame.dlc == 0xFFFF)
    {
      Logger.Log(LogLevel.Error, $"Error while processing the CAN frame dlc. The frame is ignored due to inconsistent " +
        $"frame size. DataLength is: {dataLength}");
      return new CanFrame();
    }
    // 3 next fields only used for can XL
    silkitCanFrame.sdt = 0;
    silkitCanFrame.vcid = 0;
    silkitCanFrame.af = 0;

    // handle data field. Starts from the 17th byte
    var bytes = LSCanFrame.AsSpan(16, dataLength);
    var dataptr = Marshal.AllocHGlobal(dataLength);
    Marshal.Copy(bytes.ToArray(), 0, dataptr, dataLength);

    silkitCanFrame.data = new ByteVector
    {
      data = dataptr,
      size = (IntPtr)dataLength
    };
    
    return silkitCanFrame;
  }

#endregion FMU -> SIL Kit (plain)

  // serialization with type conversion
  private void SerializeToSilKit(
    object[] objectArray,
    OptionalType targetType,
    int[]? valueSizes,
    ulong[]? dimensions,
    Serializer serializer)
  {
    // begin optional

    if (targetType.IsOptional)
    {
      serializer.BeginOptional(true);
    }

    if (targetType.IsList == true)
    {
      if (targetType.InnerType == null)
      {
        throw new InvalidCommunicationInterfaceException(
          $"Warning: The parsed object '{nameof(objectArray)}' contained a list without an inner type. This is not allowed.");
      }

      if (targetType.InnerType.IsList == true && dimensions != null && dimensions.Length > 1)
      {
        SerializeMultidimensionalArrayWithOptional(objectArray, targetType, valueSizes, dimensions, 0, 0, serializer);
      }
      else
      {
        // if list -> add array header
        serializer.BeginArray(objectArray.Length);

        if (targetType.InnerType.IsList == true)
        {
          // nested list without dimensions - each element is already a subarray
          foreach (var o in objectArray)
          {
            var entry = new object[] { o };
            SerializeToSilKit(entry, targetType.InnerType, valueSizes, null, serializer);
          }
        }
        else
        {
          if (targetType.InnerType.Type == null)
          {
            throw new NotSupportedException(
              $"The serialization of non-built-in inner types is currently not supported. " +
              $"Exception thrown by {nameof(objectArray)}");
          }

          // flat list: serialize each element directly
          foreach (var o in objectArray)
          {
            var entry = new object[] { o };
            SerializeToSilKit(entry, targetType.InnerType, valueSizes, null, serializer);
          }
        }

        // if list -> end list
        serializer.EndArray();
      }

      // if list -> end list
      serializer.EndArray();
    }
    else
    {
      // Custom types must be handled before serialization
      // -> Throw an exception is this case
      if (targetType.Type == null)
      {
        throw new NotSupportedException($"The serialization of non-built-in types is currently not supported. " +
          $"Exception thrown by {nameof(objectArray)}");
      }

      SerializeToSilKit(objectArray, true, false, targetType.Type, valueSizes, serializer);
    }

    // end optional
    if (targetType.IsOptional)
    {
      serializer.EndOptional();
    }
  }


  private void SerializeToSilKit(
    object[] objectArray,
    bool isScalar,
    bool serializeRawData,
    Type sourceType,
    int[]? valueSizes,
    Serializer serializer)
  {
    if (isScalar && objectArray.Length > 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(objectArray),
        "the encoded data was supposed to be scalar, but had more than one entry to encode.");
    }

    if (sourceType != typeof(IntPtr) && sourceType != typeof(byte[]))
    {
      SerializeToSilKit(objectArray, isScalar, sourceType, serializer, null);
      return;
    }

    if (valueSizes == null || valueSizes.Length != objectArray.Length)
    {
      throw new ArgumentException($"{nameof(objectArray)}: valueSizes was either null or did not match the size of objectArray");
    }

    // Binaries and binary arrays are special as they need additional information regarding their size
    if (!isScalar)
    {
      serializer.BeginArray(objectArray.Length);
    }

    for (var i = 0; i < objectArray.Length; i++)
    {
      var binDataPtr = (IntPtr)objectArray[i];
      var rawDataLength = valueSizes[i];
      var binData = new byte[rawDataLength];
      if (binDataPtr != 0)
      {
        Marshal.Copy(binDataPtr, binData, 0, rawDataLength);
      }
      else
      {
        Array.Fill<byte>(binData, 0);
      }
      
      if (serializeRawData)
      {
        serializer.SerializeRaw(binData);
      }
      else
      {
        serializer.Serialize(binData);
      }      
    }

    if (!isScalar)
    {
      serializer.EndArray();
    }
  }

  private void SerializeToSilKit(
    object[] objectArray, Type sourceType, Serializer serializer)
  {
    if (sourceType == typeof(float))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToSingle);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(double))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToDouble);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(sbyte))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToSByte);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(short))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt16);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(int))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt32);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(long) || sourceType == typeof(Enum))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt64);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(byte))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToByte);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(ushort))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt16);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(uint))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt32);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(ulong))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt64);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(bool))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToBoolean);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(string))
    {
      // NB: It is sufficient to check the first element, as all elements of the array are of the same type
      // (it's an list of a specific type not of object as the code may suggest)
      if (objectArray[0].GetType() != typeof(string))
      {
        throw new NotSupportedException($"Strings cannot be converted to or from other types. Exception thrown by " +
          $"{nameof(objectArray)}");
      }

      // strings cannot be convert
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToString);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i] ?? "");
      }

      return;
    }

    if (sourceType == typeof(IntPtr))
    {
      throw new NotSupportedException($"Binaries cannot be converted to or from other types. Exception thrown by " +
        $"{nameof(objectArray)}");
    }

    throw new NotSupportedException($"Unknown data type ('{sourceType.Name}').");
  }

#endregion FMU -> SIL Kit
}
