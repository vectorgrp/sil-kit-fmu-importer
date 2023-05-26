// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;

namespace Fmi;

public class Helpers
{
  public enum LogSeverity
  {
    Error,
    Warning,
    Information,
    Debug,
    Trace
  }

  private static Action<LogSeverity, string>? _loggerAction;

  public static void SetLoggerCallback(Action<LogSeverity, string> callback)
  {
    _loggerAction = callback;
  }

  public static void Log(LogSeverity severity, string message)
  {
    _loggerAction?.Invoke(severity, message);
  }

  public static byte[] EncodeData(object data, VariableTypes type, ref List<int> binSizes)
  {
    // there are no binSizes by default
    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
      {
        if (data is string s)
        {
          var success = float.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as float, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((float)data);
      }
      case VariableTypes.Float64:
      {
        if (data is string s)
        {
          var success = double.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as double, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((double)data);
      }
      case VariableTypes.Int8:
      {
        if (data is string s)
        {
          var success = sbyte.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as sbyte, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((sbyte)data);
      }
      case VariableTypes.Int16:
      {
        if (data is string s)
        {
          var success = Int16.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as Int16, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((Int16)data);
      }
      case VariableTypes.Int32:
      {
        if (data is string s)
        {
          var success = Int32.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as Int32, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((Int32)data);
      }
      case VariableTypes.Int64:
      {
        if (data is string s)
        {
          var success = Int64.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as Int64, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((Int64)data);
      }
      case VariableTypes.UInt8:
      {
        if (data is string s)
        {
          var success = byte.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as byte, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((byte)data);
      }
      case VariableTypes.UInt16:
      {
        if (data is string s)
        {
          var success = UInt16.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as UInt16, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((UInt16)data);
      }
      case VariableTypes.UInt32:
      {
        if (data is string s)
        {
          var success = UInt32.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as UInt32, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((UInt32)data);
      }
      case VariableTypes.UInt64:
      {
        if (data is string s)
        {
          var success = UInt64.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as UInt64, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((UInt64)data);
      }
      case VariableTypes.Boolean:
      {
        if (data is string s)
        {
          if (s.ToLower() is "true" or "1")
          {
            return BitConverter.GetBytes(true);
          }

          if (s.ToLower() is "false" or "0")
          {
            return BitConverter.GetBytes(true);
          }

          throw new BadImageFormatException(
            $"The string '{s}' was declared as bool, which could not be parsed as such");
        }

        return BitConverter.GetBytes((bool)data);
      }
      case VariableTypes.String:
      {
        var encodedString = Encoding.UTF8.GetBytes((string)data);
        var byteCount = encodedString.Length;
        var res = new List<byte>(BitConverter.GetBytes(byteCount));
        res.AddRange(encodedString);
        return res.ToArray();
      }
      case VariableTypes.Binary:
      {
        // this is a binary - convert the input to a proper byte array
        // the converter will initially think that the data is a string -> convert it to a byte array
        var result = Convert.FromHexString((string)data);
        binSizes.Add(result.Length);
        return result;
      }
      case VariableTypes.EnumFmi2:
      {
        if (data is string s)
        {
          var success = Int32.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as Enum (32 bit), which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((Int32)data);
      }
      case VariableTypes.EnumFmi3:
      {
        if (data is string s)
        {
          var success = Int64.TryParse(s, out var result);
          if (!success)
          {
            throw new BadImageFormatException(
              $"The string '{s}' was declared as Enum (64 bit), which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        return BitConverter.GetBytes((Int64)data);
      }
      default:
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    throw new NotSupportedException("Received unknown data type for conversion");
  }

  public static byte[] EncodeData(List<object> list, VariableTypes type, ref List<int> binSizes)
  {
    var result = new List<byte>();
    result.AddRange(BitConverter.GetBytes(list.Count));
    foreach (var entry in list)
    {
      result.AddRange(EncodeData(entry, type, ref binSizes));
    }

    return result.ToArray();
  }
}
