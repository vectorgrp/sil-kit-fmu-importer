// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Supplements;

public class SerDes
{
  public static void Serialize(
    object[] objectArray,
    bool isScalar,
    Type sourceType,
    int[]? valueSizes,
    ref Serializer serializer)
  {
    if (isScalar && objectArray.Length > 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(objectArray),
        "the encoded data was supposed to be scalar, but had more than one entry to encode.");
    }

    if (sourceType != typeof(IntPtr) && sourceType != typeof(byte[]))
    {
      Serialize(objectArray, isScalar, sourceType, ref serializer);
      return;
    }

    if (valueSizes == null || valueSizes.Length != objectArray.Length)
    {
      throw new ArgumentException("valueSizes was either null or did not match the size of objectArray");
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
      Marshal.Copy(binDataPtr, binData, 0, rawDataLength);

      serializer.Serialize(binData);

      if (!isScalar)
      {
        serializer.EndArray();
      }
    }
  }

  public static void Serialize(object[] objectArray, bool isScalar, Type sourceType, ref Serializer serializer)
  {
    if (!isScalar)
    {
      serializer.BeginArray(objectArray.Length);
    }

    Serialize(objectArray, sourceType, ref serializer);
    if (!isScalar)
    {
      serializer.EndArray();
    }
  }

  private static void Serialize(object[] objectArray, Type sourceType, ref Serializer serializer)
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

    if (sourceType == typeof(Int16))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt16);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(Int32))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt32);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(Int64) || sourceType == typeof(Enum))
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

    if (sourceType == typeof(UInt16))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt16);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(UInt32))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt32);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i]);
      }

      return;
    }

    if (sourceType == typeof(UInt64))
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
      if (objectArray[0].GetType() != typeof(string))
      {
        throw new NotSupportedException("Strings cannot be converted to or from other types");
      }

      // strings cannot be converted
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToString);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        serializer.Serialize(convertedArray[i] ?? "");
      }

      return;
    }

    if (sourceType == typeof(IntPtr))
    {
      throw new NotSupportedException("Binaries cannot be converted with this method.");
    }

    throw new NotSupportedException($"Unknown data type ('{sourceType.Name}').");
  }
}
