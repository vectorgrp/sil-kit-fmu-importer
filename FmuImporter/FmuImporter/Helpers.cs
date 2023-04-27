using FmuImporter.Config;
using System;
using System.Text;

namespace FmuImporter;

public static class Helpers
{
  /// <summary>
  /// Converts SIL Kit time (measured in nanoseconds) to FMI time (measured in seconds)
  /// </summary>
  /// <param name="silKitTimeInNs">The time used in SIL Kit (ns as ulong)</param>
  /// <returns>The time used in FMI (s as double)</returns>
  public static double SilKitTimeToFmiTime(ulong silKitTimeInNs)
  {
    return Convert.ToDouble(silKitTimeInNs / 1e9);
  }

  /// <summary>
  /// Converts FMI time (measured in seconds) to SIL Kit time (measured in nanoseconds)
  /// </summary>
  /// <param name="fmiTimeInS">The time used in FMI (s as double)</param>
  /// <returns>The time used in SIL Kit (ns as ulong)</returns>
  public static ulong FmiTimeToSilKitTime(double fmiTimeInS)
  {
    return Convert.ToUInt64(fmiTimeInS * 1e9);
  }

  public const ulong DefaultSimStepDuration = 1000000 /* 1ms */;

  public static void ToLittleEndian(ref byte[] bytes)
  {
    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
  }

  public static Type StringToType(string s)
  {
    switch (s.ToLowerInvariant())
    {
      case "uint8":
        return typeof(byte);
      case "uint16":
        return typeof(UInt16);
      case "uint32":
        return typeof(UInt32);
      case "uint64":
        return typeof(double);
      case "int8":
        return typeof(sbyte);
      case "int16":
        return typeof(Int16);
      case "int32":
        return typeof(Int32);
      case "int64":
        return typeof(Int64);
      case "float32":
      case "float":
        return typeof(float);
      case "float64":
      case "double":
        return typeof(double);
      default:
        throw new NotSupportedException($"The transformDuringTransmissionType '{s}' does not exist.");
    }
  }

  public static object FromByteArray(byte[] data, Type type, out int nextIndex)
  {
    return FromByteArray(data, type, 0, out nextIndex);
  }

  public static object FromByteArray(byte[] data, Type type, int startIndex, out int nextIndex)
  {
    if (type == typeof(float))
    {
      nextIndex = startIndex + sizeof(float);
      return BitConverter.ToSingle(data, startIndex);
    }

    if (type == typeof(double))
    {
      nextIndex = startIndex + sizeof(double);
      return BitConverter.ToDouble(data, startIndex);
    }

    if (type == typeof(byte))
    {
      nextIndex = startIndex + sizeof(byte);
      return data[startIndex];
    }

    if (type == typeof(Int16))
    {
      nextIndex = startIndex + sizeof(Int16);
      return BitConverter.ToInt16(data, startIndex);
    }

    if (type == typeof(Int32))
    {
      nextIndex = startIndex + sizeof(Int32);
      return BitConverter.ToInt32(data, startIndex);
    }

    if (type == typeof(Int64))
    {
      nextIndex = startIndex + sizeof(Int64);
      return BitConverter.ToInt64(data, startIndex);
    }

    if (type == typeof(sbyte))
    {
      nextIndex = startIndex + sizeof(sbyte);
      return (sbyte)(data[startIndex]);
    }

    if (type == typeof(UInt16))
    {
      nextIndex = startIndex + sizeof(UInt16);
      return BitConverter.ToUInt16(data, startIndex);
    }

    if (type == typeof(UInt32))
    {
      nextIndex = startIndex + sizeof(UInt32);
      return BitConverter.ToUInt32(data, startIndex);
    }

    if (type == typeof(UInt64))
    {
      nextIndex = startIndex + sizeof(UInt64);
      return BitConverter.ToUInt64(data, startIndex);
    }

    if (type == typeof(bool))
    {
      nextIndex = startIndex + sizeof(bool);
      return BitConverter.ToBoolean(data, startIndex);
    }

    if (type == typeof(string))
    {
      var stringLength = BitConverter.ToInt32(data, startIndex);
      nextIndex = startIndex + stringLength;
      return Encoding.UTF8.GetString(data, startIndex + 4, stringLength);
    }

    throw new NotSupportedException($"Failed to convert byte array into requested type '{type.Name}'");
  }

  public static byte[] ToByteArray(object[] objectArray, Type type)
  {
    var byteList = new List<byte>();

    if (type == typeof(float))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (float)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(double))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (double)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(byte))
    {
      return Array.ConvertAll(objectArray, e => (byte)e);
    }

    if (type == typeof(Int16))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (Int16)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(Int32))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (Int32)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(Int64))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (Int64)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(sbyte))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (sbyte)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(UInt16))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (UInt16)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(UInt32))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (UInt32)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    if (type == typeof(UInt64))
    {
      var convertedArray = Array.ConvertAll(objectArray, e => (UInt64)e);
      for (int i = 0; i < convertedArray.Length; i++)
      {
        byteList.AddRange(BitConverter.GetBytes(convertedArray[i]));
      }

      return byteList.ToArray();
    }

    throw new NotSupportedException("Unknown data. TODO improve message; extend method");
  }

  public static void ApplyLinearTransformation(ref object o, double factor, double offset, Type type)
  {
    if (type == typeof(float))
    {
      o = (float)((float)o * factor + offset);
      return;
    }

    if (type == typeof(double))
    {
      o = (double)o * factor + offset;
      return;
    }

    if (type == typeof(byte))
    {
      o = (byte)((byte)o * factor + offset);
      return;
    }

    if (type == typeof(Int16))
    {
      o = (Int16)((Int16)o * factor + offset);
      return;
    }

    if (type == typeof(Int32))
    {
      o = (Int32)((Int32)o * factor + offset);
      return;
    }

    if (type == typeof(Int64))
    {
      o = (Int64)((Int64)o * factor + offset);
      return;
    }

    if (type == typeof(sbyte))
    {
      o = (sbyte)((sbyte)o * factor + offset);
      return;
    }

    if (type == typeof(UInt16))
    {
      o = (UInt16)((UInt16)o * factor + offset);
      return;
    }

    if (type == typeof(UInt32))
    {
      o = (UInt32)((UInt32)o * factor + offset);
      return;
    }

    if (type == typeof(UInt64))
    {
      o = (UInt64)((UInt64)o * factor + offset);
      return;
    }

    throw new NotSupportedException("The provided type '' cannot be changed using a factor or offset");
  }
}
