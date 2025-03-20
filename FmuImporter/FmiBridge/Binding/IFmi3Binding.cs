// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Binding.Helper;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public interface IFmi3Binding : IFmiBindingCommon
{
  public ModelDescription GetModelDescription();

  // Common & Co-Simulation Functions for FMI 3.0
  public void InstantiateCoSimulation(
    string instanceName,
    string instantiationToken,
    bool visible,
    bool loggingOn,
    bool eventModeUsed,
    Fmi3LogMessageCallback logger);

  public void EnterConfigurationMode();
  public void ExitConfigurationMode();
  public void EnterInitializationMode(double? tolerance, double startTime, double? stopTime);
  public void ExitInitializationMode();

  public void EnterEventMode();

  public void UpdateDiscreteStates(out bool discreteStatesNeedUpdate, out bool terminateSimulation);

  public void EnterStepMode();

  public void SetDebugLogging(
    bool loggingOn,
    int nCategories,
    string[]? categories);

  // Getters & Setters
  public ReturnVariable GetFloat32(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetFloat64(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt8(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt8(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt16(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt16(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt32(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt32(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt64(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt64(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetBoolean(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetClock(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetString(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetBinary(fmi3ValueReference[] valueReferences);
  public void SetFloat32(fmi3ValueReference[] valueReferences, float[] values);
  public void SetFloat64(fmi3ValueReference[] valueReferences, double[] values);
  public void SetInt8(fmi3ValueReference[] valueReferences, sbyte[] values);
  public void SetUInt8(fmi3ValueReference[] valueReferences, byte[] values);
  public void SetInt16(fmi3ValueReference[] valueReferences, short[] values);
  public void SetUInt16(fmi3ValueReference[] valueReferences, ushort[] values);
  public void SetInt32(fmi3ValueReference[] valueReferences, int[] values);
  public void SetUInt32(fmi3ValueReference[] valueReferences, uint[] values);
  public void SetInt64(fmi3ValueReference[] valueReferences, long[] values);
  public void SetUInt64(fmi3ValueReference[] valueReferences, ulong[] values);
  public void SetBoolean(fmi3ValueReference[] valueReferences, bool[] values);
  public void SetClock(fmi3ValueReference[] valueReferences, bool[] values);
  public void SetString(fmi3ValueReference[] valueReferences, string[] values);
  public void SetBinary(fmi3ValueReference[] valueReferences, IntPtr[] valueSizes, IntPtr[] values);
}
