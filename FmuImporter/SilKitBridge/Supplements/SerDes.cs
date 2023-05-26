// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using System.Text;

namespace SilKit.Supplements;

public class SerDes
{
  public static byte[] Serialize(object[] objectArray, bool isScalar, Type sourceType, int[]? valueSizes)
  {
    if (isScalar && objectArray.Length > 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(objectArray),
        "the encoded data was supposed to be scalar, but had more than one entry to encode.");
    }

    if (sourceType != typeof(IntPtr))
    {
      return Serialize(objectArray, isScalar, sourceType);
    }

    if (valueSizes == null || valueSizes.Length != objectArray.Length)
    {
      throw new ArgumentException("valueSizes was either null or did not match the size of objectArray");
    }

    // Binaries and binary arrays are special as they need additional information regarding their size
    List<byte> byteList;
    if (!isScalar)
    {
      byteList = GetArrayHeader(objectArray.Length);
    }
    else
    {
      byteList = new List<byte>();
    }

    for (var i = 0; i < objectArray.Length; i++)
    {
      var binDataPtr = (IntPtr)objectArray[i];
      var rawDataLength = valueSizes[i];
      var binData = new byte[rawDataLength];
      Marshal.Copy(binDataPtr, binData, 0, rawDataLength);

      // append length of following binary blob
      byteList.AddRange(GetArrayHeader(binData.Length));
      // add the data blob
      byteList.AddRange(binData);
    }

    return byteList.ToArray();
  }


  public static byte[] Serialize(object[] objectArray, bool isScalar, Type sourceType)
  {
    if (!isScalar)
    {
      var result = GetArrayHeader(objectArray.Length);
      result.AddRange(Serialize(objectArray, sourceType));
      return result.ToArray();
    }

    return Serialize(objectArray, sourceType);
  }

  private static byte[] Serialize(object[] objectArray, Type sourceType)
  {
    var byteList = new List<byte>();

    if (sourceType == typeof(float))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToSingle);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(double))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToDouble);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(byte))
    {
      return Array.ConvertAll(objectArray, Convert.ToByte);
    }

    if (sourceType == typeof(Int16))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt16);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(Int32))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt32);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(Int64) || sourceType == typeof(Enum))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToInt64);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(sbyte))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToSByte);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(UInt16))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt16);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(UInt32))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt32);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(UInt64))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToUInt64);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(bool))
    {
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToBoolean);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(string))
    {
      if (objectArray[0].GetType() != typeof(string))
      {
        throw new NotSupportedException("Strings cannot be converted to or from other types");
      }

      // strings cannot be convert
      var convertedArray = Array.ConvertAll(objectArray, Convert.ToString);
      for (var i = 0; i < convertedArray.Length; i++)
      {
        // TODO check if this conversion works as intended
        var encodedString = Encoding.UTF8.GetBytes((string)(objectArray[i]));
        var byteCount = encodedString.Length;
        var res = new List<byte>(BitConverter.GetBytes(byteCount));
        res.AddRange(encodedString);
        byteList.AddRange(res);
      }

      return byteList.ToArray();
    }

    if (sourceType == typeof(IntPtr))
    {
      throw new NotSupportedException("TODO Binaries cannot be converted like this!");
    }

    throw new NotSupportedException("Unknown data. TODO improve message; extend method");
  }

  public static List<byte> GetArrayHeader(int arrayLength)
  {
    var byteArr = BitConverter.GetBytes(arrayLength);
    ToLittleEndian(ref byteArr);

    return byteArr.ToList();
  }

  private static void ToLittleEndian(ref byte[] bytes)
  {
    if (!BitConverter.IsLittleEndian)
    {
      Array.Reverse(bytes);
    }
  }
}
