// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using FmuImporter.Config;
using SilKit.Services.Logger;

namespace FmuImporter.Helpers;

public static class Helpers
{
  /// <summary>
  ///   Converts severity levels between the ones defined in the FMU bridge and the ones in SIL Kit
  /// </summary>
  /// <param name="logSeverity">The severity log level from FMI</param>
  /// <returns>The severity log level in SIL Kit</returns>
  /// <exception cref="ArgumentOutOfRangeException">An unknown severity level was provided</exception>
  public static LogLevel FmiLogLevelToSilKitLogLevel(LogSeverity logSeverity)
  {
    switch (logSeverity)
    {
      case LogSeverity.Error:
        return LogLevel.Error;
      case LogSeverity.Warning:
        return LogLevel.Warn;
      case LogSeverity.Information:
        return LogLevel.Info;
      case LogSeverity.Debug:
        return LogLevel.Debug;
      case LogSeverity.Trace:
        return LogLevel.Trace;
      default:
        throw new ArgumentOutOfRangeException(nameof(logSeverity), logSeverity, null);
    }
  }

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
        return typeof(byte[]);
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
        return typeof(ushort);
      case "uint32":
        return typeof(uint);
      case "uint64":
        return typeof(double);
      case "int8":
        return typeof(sbyte);
      case "int16":
        return typeof(short);
      case "int32":
        return typeof(int);
      case "int64":
        return typeof(long);
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

  public static void ApplyLinearTransformationFmi(ref object deserializedData, ConfiguredVariable configuredVariable)
  {
    if (configuredVariable.FmuVariableDefinition.VariableType != VariableTypes.Float32 &&
        configuredVariable.FmuVariableDefinition.VariableType != VariableTypes.Float64)
    {
      return;
    }

    var unit = configuredVariable.FmuVariableDefinition.TypeDefinition?.Unit;
    if (unit != null && (unit.Factor.HasValue || unit.Offset.HasValue))
    {
      ApplyLinearTransformation(
        ref deserializedData,
        unit.Factor,
        unit.Offset,
        configuredVariable.FmuVariableDefinition.VariableType);
    }
  }

  public static void ApplyLinearTransformationImporterConfig(
    ref object deserializedData,
    ConfiguredVariable configuredVariable)
  {
    var transformation = configuredVariable.ImporterVariableConfiguration.Transformation;
    if (transformation != null && (transformation.Factor.HasValue || transformation.Offset.HasValue))
    {
      ApplyLinearTransformation(
        ref deserializedData,
        transformation.ComputedFactor,
        transformation.ComputedOffset,
        configuredVariable.FmuVariableDefinition.VariableType);
    }
  }

  private static void ApplyLinearTransformation(ref object o, double? factor, double? offset, VariableTypes type)
  {
    if (!factor.HasValue && !offset.HasValue)
    {
      // there is nothing to transform
      return;
    }

    factor ??= 1D;
    offset ??= 0D;

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
        o = (short)((short)o * factor + offset);
        return;
      }
      case VariableTypes.Int32:
      {
        o = (int)((int)o * factor + offset);
        return;
      }
      case VariableTypes.Int64:
      {
        o = (long)((long)o * factor + offset);
        return;
      }
      case VariableTypes.UInt8:
      {
        o = (byte)((byte)o * factor + offset);
        return;
      }
      case VariableTypes.UInt16:
      {
        o = (ushort)((ushort)o * factor + offset);
        return;
      }
      case VariableTypes.UInt32:
      {
        o = (uint)((uint)o * factor + offset);
        return;
      }
      case VariableTypes.UInt64:
      {
        o = (ulong)((ulong)o * factor + offset);
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
