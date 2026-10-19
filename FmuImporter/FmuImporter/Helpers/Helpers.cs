// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
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

  public static void LogToConsole(LogSeverity severity, string message)
  {
    Console.WriteLine(
      $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{FmiLogLevelToSilKitLogLevel(severity).ToString().ToLowerInvariant()}] {message}");
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
      case VariableTypes.TriggeredClock:
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

  public static void ApplyLinearTransformationFmi(ref object deserializedData, ConfiguredVariable configuredVariable)
  {
    if ((configuredVariable.FmuVariableDefinition.VariableType != VariableTypes.Float32) &&
        (configuredVariable.FmuVariableDefinition.VariableType != VariableTypes.Float64))
    {
      return;
    }

    var unit = configuredVariable.FmuVariableDefinition.TypeDefinition?.Unit;
    if ((unit != null) && (unit.Factor.HasValue || unit.Offset.HasValue))
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
    if ((transformation != null) && (transformation.Factor.HasValue || transformation.Offset.HasValue))
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

  /// <summary>
  ///   Checks whether the Tx clock of a FMI-LS-BUS Tx variable (e.g., Tx_Data or Tx_Id) is an input clock.
  ///   Must be called before the clock is removed from the model description variables.
  /// </summary>
  /// <exception cref="NotSupportedException">Thrown if the Tx clock is not a triggered clock.</exception>
  public static bool IsInputTxClock(Variable txVariable, Dictionary<uint, Variable> modelDescriptionVariables)
  {
    // it is assumed that one Tx variable has only one associated Tx clock
    var clockValueRef = txVariable.Clocks!.FirstOrDefault();

    // non-triggered clocks (periodic, countdown, ...) are discarded while parsing the model description
    if (!modelDescriptionVariables.TryGetValue(clockValueRef, out var clockVariable) ||
        clockVariable.VariableType != VariableTypes.TriggeredClock)
    {
      throw new NotSupportedException(
        $"The Tx clock (value reference {clockValueRef}) of variable '{txVariable.Name}' is not a triggered clock. " +
        $"Only triggered Tx clocks are supported.");
    }

    return clockVariable.Causality == Variable.Causalities.Input;
  }

  /// <summary>
  ///   Checks whether the Tx clock of a FMI-LS-BUS Tx variable is active in the current step.
  /// </summary>
  /// <param name="binding">The FMI binding used to query output Tx clocks.</param>
  /// <param name="inputTxClocks">Activation state of the input Tx clocks, keyed by clock value reference.</param>
  /// <param name="clockRef">The value reference of the Tx clock to check.</param>
  /// <returns>True if the Tx clock is active, false otherwise.</returns>
  public static bool IsActiveClock(IFmiBindingCommon binding, Dictionary<uint, bool> inputTxClocks, uint clockRef)
  {
    // input Tx clocks are activated when a value is received via SIL Kit, output Tx clocks by the FMU
    if (inputTxClocks.TryGetValue(clockRef, out var isInputTxClockActive))
    {
      if (!isInputTxClockActive)
      {
        return false;
      }

      // retrieve the Tx data only once per activation
      inputTxClocks[clockRef] = false;
    }
    else
    {
      binding.GetValue(new uint[] { clockRef }, out var clockResult, VariableTypes.TriggeredClock);
      if ((bool)clockResult.ResultArray[0].Values[0] == false)
      {
        return false;
      }
    }

    return true;
  }
}
