using System.Text;

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

  public static object FromByteArray(byte[] data, Type type, ref int currentScanIndex)
  {
    if (type == typeof(float))
    {
      var result = BitConverter.ToSingle(data, currentScanIndex);
      currentScanIndex += sizeof(float);
      return result;
    }

    if (type == typeof(double))
    {
      var result = BitConverter.ToDouble(data, currentScanIndex);
      currentScanIndex += sizeof(double);
      return result;
    }

    if (type == typeof(byte))
    {
      currentScanIndex += sizeof(byte);
      return data[currentScanIndex - sizeof(byte)];
    }

    if (type == typeof(Int16))
    {
      var result = BitConverter.ToInt16(data, currentScanIndex);
      currentScanIndex += sizeof(Int16);
      return result;
    }

    if (type == typeof(Int32))
    {
      var result = BitConverter.ToInt32(data, currentScanIndex);
      currentScanIndex += sizeof(Int32);
      return result;
    }

    if (type == typeof(Int64))
    {
      var result = BitConverter.ToInt64(data, currentScanIndex);
      currentScanIndex += sizeof(Int64);
      return result;
    }

    if (type == typeof(sbyte))
    {
      var result = (sbyte)(data[currentScanIndex]);
      currentScanIndex += sizeof(sbyte);
      return result;
    }

    if (type == typeof(UInt16))
    {
      var result = BitConverter.ToUInt16(data, currentScanIndex);
      currentScanIndex += sizeof(UInt16);
      return result;
    }

    if (type == typeof(UInt32))
    {
      var result = BitConverter.ToUInt32(data, currentScanIndex);
      currentScanIndex += sizeof(UInt32);
      return result;
    }

    if (type == typeof(UInt64))
    {
      var result = BitConverter.ToUInt64(data, currentScanIndex);
      currentScanIndex += sizeof(UInt64);
      return result;
    }

    if (type == typeof(bool))
    {
      var result = BitConverter.ToBoolean(data, currentScanIndex);
      currentScanIndex += sizeof(bool);
      return result;
    }

    if (type == typeof(string))
    {
      // NB this assumes a SIL Kit encoded string
      // string = [character count (4 byte/Int32)][chars...]
      var stringLength = BitConverter.ToInt32(data, currentScanIndex);
      var result = Encoding.UTF8.GetString(data, currentScanIndex + 4, stringLength);
      currentScanIndex += stringLength;
      return result;
    }

    throw new NotSupportedException($"Failed to convert byte array into requested type '{type.Name}'");
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
