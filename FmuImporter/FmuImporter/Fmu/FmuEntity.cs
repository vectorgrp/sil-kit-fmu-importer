﻿// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace FmuImporter.Fmu;

public class FmuEntity : IDisposable
{
  public enum FmuSuperStates
  {
    Uninitialized,
    Initialized,
    Exited
  }

  public FmiVersions FmiVersion { get; }

  public FmuSuperStates CurrentFmuSuperState { get; private set; } = FmuSuperStates.Uninitialized;
  public ModelDescription ModelDescription { get; set; }
  public IFmiBindingCommon Binding { get; set; }

  public delegate void FmiLog(LogSeverity severity, string message);

  public event FmiLog? OnFmuLog;

  private readonly string _fmuPath;

  public FmuEntity(string fmuPath)
  {
    _fmuPath = fmuPath;

    FmiVersion = ModelLoader.FindFmiVersion(fmuPath);

    Binding = BindingFactory.CreateBinding(FmiVersion, fmuPath);
    Binding.SetLoggerCallback(RaiseOnFmuLogEvent);
    ModelDescription = Binding.ModelDescription;
  }

  protected virtual void RaiseOnFmuLogEvent(LogSeverity severity, string message)
  {
    OnFmuLog?.Invoke(severity, message);
  }


  public void Fmi2StepFinished(FmiStatus status)
  {
    ((IFmi2Binding)Binding).NotifyAsyncDoStepReturned(status);
  }

  private Fmi2BindingCallbackFunctions _fmi2Functions;

  public void PrepareFmu(Action fmuInitializationAction)
  {
    switch (FmiVersion)
    {
      case FmiVersions.Fmi2:
        PrepareFmi2Fmu(_fmuPath, fmuInitializationAction);
        break;
      case FmiVersions.Fmi3:
        PrepareFmi3Fmu(_fmuPath, fmuInitializationAction);
        break;
      default:
        throw new ArgumentOutOfRangeException("The provided FMI version is unknown.");
    }

    CurrentFmuSuperState = FmuSuperStates.Initialized;
  }

  private void PrepareFmi2Fmu(string fmuPath, Action fmuInitializationAction)
  {
    var fmi2Binding = (IFmi2Binding)Binding;

    // Prepare FMU
    _fmi2Functions = new Fmi2BindingCallbackFunctions(Fmi2Logger, Fmi2StepFinished);

    fmi2Binding.Instantiate(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      _fmi2Functions,
      true,
      true);

    fmi2Binding.SetDebugLogging(true, Array.Empty<string>());

    fmi2Binding.SetupExperiment(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    fmi2Binding.EnterInitializationMode();

    fmuInitializationAction();

    fmi2Binding.ExitInitializationMode();
  }

  private void PrepareFmi3Fmu(string fmuPath, Action fmuInitializationAction)
  {
    var fmi3Binding = (IFmi3Binding)Binding;
    // Get FMI Model binding

    fmi3Binding.InstantiateCoSimulation(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      true,
      true,
      Fmi3Logger);

    fmi3Binding.SetDebugLogging(true, 0, null);

    fmi3Binding.EnterInitializationMode(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    fmuInitializationAction();

    fmi3Binding.ExitInitializationMode();
  }

  private void Fmi2Logger(
    string instanceName,
    FmiStatus status,
    string category,
    string message)
  {
    LogMessage(status, category, message);
  }

  private void Fmi3Logger(IntPtr instanceEnvironment, FmiStatus status, string category, string message)
  {
    LogMessage(status, category, message);
  }

  private void LogMessage(
    FmiStatus status,
    string category,
    string message)
  {
    var msg = $"FmuLogger: status={status}; category={category};\n  message={message}";
    switch (status)
    {
      case FmiStatus.OK:
      case FmiStatus.Pending:
        RaiseOnFmuLogEvent(LogSeverity.Information, msg);
        break;
      case FmiStatus.Discard:
      case FmiStatus.Warning:
        RaiseOnFmuLogEvent(LogSeverity.Warning, msg);
        break;
      case FmiStatus.Error:
        RaiseOnFmuLogEvent(LogSeverity.Error, msg);
        break;
      case FmiStatus.Fatal:
        RaiseOnFmuLogEvent(LogSeverity.Error, msg);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(status), status, null);
    }
  }

  public double? GetStepSize()
  {
    if (ModelDescription.DefaultExperiment.StepSize.HasValue)
    {
      return ModelDescription.DefaultExperiment.StepSize.Value;
    }

    if (ModelDescription.CoSimulation.FixedInternalStepSize.HasValue)
    {
      return ModelDescription.CoSimulation.FixedInternalStepSize.Value;
    }

    return null;
  }

  public double? GetStopTime()
  {
    return ModelDescription.DefaultExperiment?.StopTime;
  }

  public void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize)
  {
    Binding.DoStep(currentCommunicationPoint, communicationStepSize, out _);
  }

  public void Terminate()
  {
    Binding.Terminate();
    CurrentFmuSuperState = FmuSuperStates.Exited;
  }

#region IDisposable

  ~FmuEntity()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool _disposedValue;

  protected virtual void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects

        // cleanup FMU
        Binding.Dispose();
        OnFmuLog = null;
      }

      ReleaseUnmanagedResources();
      _disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

#endregion IDisposable
}