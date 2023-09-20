// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi.Exceptions;

namespace Fmi.Supplements;

public class Serializer
{
  public static byte[] Serialize(object data, VariableTypes type, ref List<int> binSizes)
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
            throw new DataConversionException(
              $"The string '{s}' was declared as float, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is float correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      case VariableTypes.Float64:
      {
        if (data is string s)
        {
          var success = double.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as double, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is double correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      case VariableTypes.Int8:
      {
        if (data is string s)
        {
          var success = sbyte.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as sbyte, which could not be parsed as such.");
          }

          return new[] { (byte)result };
        }

        if (data is sbyte correctType)
        {
          return new[] { (byte)correctType };
        }

        break;
      }
      case VariableTypes.Int16:
      {
        if (data is string s)
        {
          var success = Int16.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
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
            throw new DataConversionException(
              $"The string '{s}' was declared as Int32, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is Int32 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      case VariableTypes.Int64:
      {
        if (data is string s)
        {
          var success = Int64.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as Int64, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is Int64 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      case VariableTypes.UInt8:
      {
        if (data is string s)
        {
          var success = byte.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as byte, which could not be parsed as such.");
          }

          return new[] { result };
        }

        if (data is byte correctType)
        {
          return new[] { correctType };
        }

        break;
      }
      case VariableTypes.UInt16:
      {
        if (data is string s)
        {
          var success = UInt16.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as UInt16, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is UInt16 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      case VariableTypes.UInt32:
      {
        if (data is string s)
        {
          var success = UInt32.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as UInt32, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is UInt32 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      case VariableTypes.UInt64:
      {
        if (data is string s)
        {
          var success = UInt64.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as UInt64, which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is UInt64 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
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

          throw new DataConversionException(
            $"The string '{s}' was declared as bool, which could not be parsed as such");
        }

        if (data is bool correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
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
        if (data is string s)
        {
          var result = Convert.FromHexString(s);
          binSizes.Add(result.Length);
          return result;
        }

        if (data is byte[] correctType)
        {
          binSizes.Add(correctType.Length);
          return correctType;
        }

        break;
      }
      case VariableTypes.EnumFmi2:
      {
        if (data is string s)
        {
          var success = Int32.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as Enum (32 bit), which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is Int64 correctType)
        {
          // SIL Kit exchanges all enums as Int64 - try to convert to 32 bit for FMI 2.0.x
          if (correctType > Int32.MaxValue)
          {
            throw new InvalidOperationException(
              $"Failed to set enumerator. " +
              $"Reason: The value is too large (> Int32) and therefore cannot be handled by FMI 2.0 FMUs.");
          }

          return BitConverter.GetBytes(Convert.ToInt32(correctType));
        }

        break;
      }
      case VariableTypes.EnumFmi3:
      {
        if (data is string s)
        {
          var success = Int64.TryParse(s, out var result);
          if (!success)
          {
            throw new DataConversionException(
              $"The string '{s}' was declared as Enum (64 bit), which could not be parsed as such.");
          }

          return BitConverter.GetBytes(result);
        }

        if (data is Int64 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        break;
      }
      default:
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    throw new NotSupportedException("Received unknown data type for conversion");
  }

  public static byte[] Serialize(List<object> list, VariableTypes type, ref List<int> binSizes)
  {
    var result = new List<byte>();
    result.AddRange(BitConverter.GetBytes(list.Count));
    foreach (var entry in list)
    {
      result.AddRange(Serialize(entry, type, ref binSizes));
    }

    return result.ToArray();
  }
}
