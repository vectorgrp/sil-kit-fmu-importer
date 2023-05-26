// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi;

namespace FmuImporter;

public static class Helpers
{
  /// <summary>
  ///   Converts SIL Kit time (measured in nanoseconds) to FMI time (measured in seconds)
  /// </summary>
  /// <param name="silKitTimeInNs">The time used in SIL Kit (ns as ulong)</param>
  /// <returns>The time used in FMI (s as double)</returns>
  public static double SilKitTimeToFmiTime(ulong silKitTimeInNs)
  {
    return Convert.ToDouble(silKitTimeInNs / 1e9);
  }

  /// <summary>
  ///   Converts FMI time (measured in seconds) to SIL Kit time (measured in nanoseconds)
  /// </summary>
  /// <param name="fmiTimeInS">The time used in FMI (s as double)</param>
  /// <returns>The time used in SIL Kit (ns as ulong)</returns>
  public static ulong FmiTimeToSilKitTime(double fmiTimeInS)
  {
    return Convert.ToUInt64(fmiTimeInS * 1e9);
  }

  public const ulong DefaultSimStepDuration = 1000000 /* 1ms */;

  public static VariableTypes StringToVariableType(string s)
  {
    switch (s.ToLowerInvariant())
    {
      case "uint8":
        return VariableTypes.UInt8;
      case "uint16":
        return VariableTypes.UInt16;
      case "uint32":
        return VariableTypes.UInt32;
      case "uint64":
        return VariableTypes.UInt64;
      case "int8":
        return VariableTypes.Int8;
      case "int16":
        return VariableTypes.Int16;
      case "int32":
        return VariableTypes.Int32;
      case "int64":
        return VariableTypes.Int64;
      case "float32":
      case "float":
        return VariableTypes.Float32;
      case "float64":
      case "double":
        return VariableTypes.Float64;
      default:
        throw new NotSupportedException($"The transformDuringTransmissionType '{s}' does not exist.");
    }
  }

  public static Type VariableTypeToType(VariableTypes type)
  {
    switch (type)
    {
      case VariableTypes.Float32:
        return typeof(float);
      case VariableTypes.Float64:
        return typeof(double);
      case VariableTypes.Int8:
        return typeof(sbyte);
      case VariableTypes.Int16:
        return typeof(short);
      case VariableTypes.Int32:
        return typeof(int);
      case VariableTypes.Int64:
        return typeof(long);
      case VariableTypes.UInt8:
        return typeof(byte);
      case VariableTypes.UInt16:
        return typeof(ushort);
      case VariableTypes.UInt32:
        return typeof(uint);
      case VariableTypes.UInt64:
        return typeof(ulong);
      case VariableTypes.Boolean:
        return typeof(bool);
      case VariableTypes.String:
        return typeof(string);
      case VariableTypes.Binary:
        return typeof(IntPtr);
      case VariableTypes.EnumFmi2:
        return typeof(Enum);
      case VariableTypes.EnumFmi3:
        return typeof(Enum);
      case VariableTypes.Undefined:
      default:
        break;
    }

    throw new ArgumentOutOfRangeException(nameof(type), type, null);
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

  public static object FromByteArray(byte[] data, VariableTypes type, ref int currentScanIndex)
  {
    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
      {
        var result = BitConverter.ToSingle(data, currentScanIndex);
        currentScanIndex += sizeof(float);
        return result;
      }
      case VariableTypes.Float64:
      {
        var result = BitConverter.ToDouble(data, currentScanIndex);
        currentScanIndex += sizeof(double);
        return result;
      }
      case VariableTypes.Int8:
      {
        var result = (sbyte)(data[currentScanIndex]);
        currentScanIndex += sizeof(sbyte);
        return result;
      }
      case VariableTypes.Int16:
      {
        var result = BitConverter.ToInt16(data, currentScanIndex);
        currentScanIndex += sizeof(Int16);
        return result;
      }
      case VariableTypes.Int32:
      {
        var result = BitConverter.ToInt32(data, currentScanIndex);
        currentScanIndex += sizeof(Int32);
        return result;
      }
      case VariableTypes.Int64:
      {
        var result = BitConverter.ToInt64(data, currentScanIndex);
        currentScanIndex += sizeof(Int64);
        return result;
      }
      case VariableTypes.UInt8:
      {
        currentScanIndex += sizeof(byte);
        return data[currentScanIndex - sizeof(byte)];
      }
      case VariableTypes.UInt16:
      {
        var result = BitConverter.ToUInt16(data, currentScanIndex);
        currentScanIndex += sizeof(UInt16);
        return result;
      }
      case VariableTypes.UInt32:
      {
        var result = BitConverter.ToUInt32(data, currentScanIndex);
        currentScanIndex += sizeof(UInt32);
        return result;
      }
      case VariableTypes.UInt64:
      {
        var result = BitConverter.ToUInt64(data, currentScanIndex);
        currentScanIndex += sizeof(UInt64);
        return result;
      }
      case VariableTypes.Boolean:
      {
        var result = BitConverter.ToBoolean(data, currentScanIndex);
        currentScanIndex += sizeof(bool);
        return result;
      }
      case VariableTypes.String:
      {
        // NB this assumes a SIL Kit encoded string
        // string = [character count (4 byte/Int32)][chars...]
        var stringLength = BitConverter.ToInt32(data, currentScanIndex);
        var result = Encoding.UTF8.GetString(data, currentScanIndex + 4, stringLength);
        currentScanIndex += stringLength;
        return result;
      }
      case VariableTypes.Binary:
        // TODO
        break;
      case VariableTypes.EnumFmi2:
      {
        // Enums are received as 64 bit values
        var result = BitConverter.ToInt64(data, currentScanIndex);
        currentScanIndex += sizeof(Int64);
        return result;
      }
      case VariableTypes.EnumFmi3:
      {
        var result = BitConverter.ToInt64(data, currentScanIndex);
        currentScanIndex += sizeof(Int64);
        return result;
      }
      default:
        break;
    }

    throw new ArgumentOutOfRangeException(
      nameof(type),
      type,
      $"Failed to convert byte array into requested type '{type}'");
  }

  public static void ApplyLinearTransformation(ref object o, double factor, double offset, VariableTypes type)
  {
    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
      {
        o = (float)((float)o * factor + offset);
        return;
      }
      case VariableTypes.Float64:
      {
        o = (double)o * factor + offset;
        return;
      }
      case VariableTypes.Int8:
      {
        o = (sbyte)((sbyte)o * factor + offset);
        return;
      }
      case VariableTypes.Int16:
      {
        o = (Int16)((Int16)o * factor + offset);
        return;
      }
      case VariableTypes.Int32:
      {
        o = (Int32)((Int32)o * factor + offset);
        return;
      }
      case VariableTypes.Int64:
      {
        o = (Int64)((Int64)o * factor + offset);
        return;
      }
      case VariableTypes.UInt8:
      {
        o = (byte)((byte)o * factor + offset);
        return;
      }
      case VariableTypes.UInt16:
      {
        o = (UInt16)((UInt16)o * factor + offset);
        return;
      }
      case VariableTypes.UInt32:
      {
        o = (UInt32)((UInt32)o * factor + offset);
        return;
      }
      case VariableTypes.UInt64:
      {
        o = (UInt64)((UInt64)o * factor + offset);
        return;
      }
      case VariableTypes.Boolean:
        break;
      case VariableTypes.String:
        break;
      case VariableTypes.Binary:
        break;
      case VariableTypes.EnumFmi2:
        break;
      case VariableTypes.EnumFmi3:
        break;
      default:
        break;
    }

    throw new ArgumentOutOfRangeException(
      nameof(type),
      type,
      $"The provided type '{type}' cannot be changed using a factor or offset");
  }
}
