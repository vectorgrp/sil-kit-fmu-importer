// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Binding.Helper;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public interface IFmiBindingCommon : IDisposable
{
  public ModelDescription ModelDescription { get; }
  public InternalFmuStates CurrentState { get; }

  public void GetValue(uint[] valueRefs, out ReturnVariable result, VariableTypes type);

  public void SetValue(uint valueRef, byte[] data);
  public void SetValue(uint valueRef, byte[] data, int[] binSizes);

  public void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime);

  public void Terminate();

  public void FreeInstance();

  public FmiVersions GetFmiVersion();

  public void SetLoggerCallback(Action<LogSeverity, string> callback);
}
