// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi.Exceptions;
using Fmi.FmiModel.Internal;

namespace Fmi.Supplements;

public class Serializer
{
  public static byte[] Serialize(object data, Variable variable, ref List<int> binSizes)
  {
    // there are no binSizes by default
    switch (variable.VariableType)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
      {
        if (data is float compatibleTypeF)
        {
          return BitConverter.GetBytes(compatibleTypeF);
        }

        if (data is double compatibleTypeD)
        {
          return BitConverter.GetBytes((float)compatibleTypeD);
        }

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

        // Managing all integer types
        return BitConverter.GetBytes((float)Convert.ToDouble(data));
      }
      case VariableTypes.Float64:
      {
        if (data is double correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        // Fallback for all other types, will fail if type is not convertible to double
        return BitConverter.GetBytes(Convert.ToDouble(data));
      }
      case VariableTypes.Int8:
      {
        if (data is sbyte correctType)
        {
          return new[] { (byte)correctType };
        }

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

        return new[] { (byte)Convert.ToSByte(data) };
      }
      case VariableTypes.Int16:
      {
        if (data is Int16 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        return BitConverter.GetBytes(Convert.ToInt16(data));
      }
      case VariableTypes.Int32:
      {
        if (data is Int32 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        return BitConverter.GetBytes(Convert.ToInt32(data));
      }
      case VariableTypes.Int64:
      {
        if (data is Int64 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        return BitConverter.GetBytes(Convert.ToInt64(data));
      }
      case VariableTypes.UInt8:
      {
        if (data is byte correctType)
        {
          return new[] { correctType };
        }

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

        return new[] { Convert.ToByte(data) };
      }
      case VariableTypes.UInt16:
      {
        if (data is UInt16 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        return BitConverter.GetBytes(Convert.ToUInt16(data));
      }
      case VariableTypes.UInt32:
      {
        if (data is UInt32 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        return BitConverter.GetBytes(Convert.ToUInt32(data));
      }
      case VariableTypes.UInt64:
      {
        if (data is UInt64 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

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

        return BitConverter.GetBytes(Convert.ToUInt64(data));
      }
      case VariableTypes.Boolean:
      {
        if (data is bool correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        if (data is string s)
        {
          if (s.ToLower() is "true" or "1")
          {
            return BitConverter.GetBytes(true);
          }

          if (s.ToLower() is "false" or "0")
          {
            return BitConverter.GetBytes(false);
          }
        }

        throw new DataConversionException(
          $"The scalar '{data}' was declared as bool, which could not be parsed as such");
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
        if (data is byte[] correctType)
        {
          binSizes.Add(correctType.Length);
          return correctType;
        }

        if (data is string s)
        {
          var result = Convert.FromHexString(s);
          binSizes.Add(result.Length);
          return result;
        }

        break;
      }
      case VariableTypes.EnumFmi2:
      {
        if (data is Int64 correctType)
        {
          // SIL Kit exchanges all enums as Int64 - try to convert to 32 bit for FMI 2.0.x
          if (correctType > Int32.MaxValue || correctType < UInt32.MinValue)
          {
            throw new InvalidOperationException(
              $"Failed to set enumerator. " +
              $"Reason: The value is too large (> Int32) and therefore cannot be handled by FMI 2.0 FMUs.");
          }

          return BitConverter.GetBytes(correctType);
        }

        if (data is string s)
        {
          var eValue = GetEnumValue(s, variable);

          // Remark: checking for overflow in Int64 -> Int32 should be superfluous
          //         as the values in 'variable' come from the Model Description.
          return BitConverter.GetBytes((Int32)eValue);
        }

        return BitConverter.GetBytes(Convert.ToInt32(data));
      }
      case VariableTypes.EnumFmi3:
      {
        if (data is Int64 correctType)
        {
          return BitConverter.GetBytes(correctType);
        }

        if (data is string s)
        {
          var eValue = GetEnumValue(s, variable);

          return BitConverter.GetBytes(eValue);
        }

        return BitConverter.GetBytes(Convert.ToInt64(data));
      }
      default:
        throw new ArgumentOutOfRangeException(
          nameof(variable.VariableType),
          variable.VariableType,
          $"Variable '{variable.Name}' has an unsupported type ({variable.VariableType.ToString()}).");
    }

    throw new NotSupportedException("Received unknown data type for conversion");
  }

  private static Int64 GetEnumValue(string enumNameInput, Variable v)
  {
    if (v.TypeDefinition == null || (v.TypeDefinition.EnumerationValues is var eValues && eValues == null))
    {
      throw new ModelDescriptionException(
        $"The type of {v.Name} lacks a proper enumeration definition.");
    }

    var enumValuesFound = eValues.Where(v => v.Item1 == enumNameInput).Select(v => v.Item2);
    var numFound = enumValuesFound.Count();
    if (numFound == 1)
    {
      return enumValuesFound.First();
    }
    else
    {
      throw new DataConversionException(
        $"The string '{enumNameInput}' mentions an enum of {v.TypeDefinition.Name} which does not exist.");
    }
    // The case of enumValuesFound > 1 would still possible if the user deviated for the standard
  }

  public static byte[] Serialize(List<object> list, Variable variable, ref List<int> binSizes)
  {
    var result = new List<byte>();
    foreach (var entry in list)
    {
      result.AddRange(Serialize(entry, variable, ref binSizes));
    }

    return result.ToArray();
  }
}
