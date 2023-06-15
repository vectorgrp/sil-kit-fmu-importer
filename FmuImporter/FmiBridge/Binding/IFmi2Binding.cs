// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public interface IFmi2Binding : IFmiBindingCommon
{
  public ModelDescription GetModelDescription();

  // Common functions
  public void SetDebugLogging(bool loggingOn, string[] categories);

  public void Instantiate(
    string instanceName,
    string fmuGUID,
    Fmi2BindingCallbackFunctions functions,
    bool visible,
    bool loggingOn);

  public void SetupExperiment(double? tolerance, double startTime, double? stopTime);
  public void EnterInitializationMode();
  public void ExitInitializationMode();

  // Functions for FMI2 for Co-Simulation
  public void CancelStep();

  // Getters & Setters
  public double[] GetReal(uint[] valueReferences);
  public int[] GetInteger(uint[] valueReferences);
  public bool[] GetBoolean(uint[] valueReferences);
  public string[] GetString(uint[] valueReferences);
  public void SetReal(uint[] valueReferences, double[] values);
  public void SetInteger(uint[] valueReferences, int[] values);
  public void SetBoolean(uint[] valueReferences, bool[] values);
  public void SetString(uint[] valueReferences, string[] values);

  // Internal
  public void NotifyAsyncDoStepReturned(Fmi2Statuses status);
}
