// SPDX-License-Identifier: MIT
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
    Instantiated,
    Initializing,
    Initialized,
    Terminated
  }

  public FmiVersions FmiVersion { get; }

  public FmuSuperStates CurrentFmuSuperState { get; private set; } = FmuSuperStates.Uninitialized;
  public ModelDescription ModelDescription { get; set; }
  public TerminalsAndIcons? TerminalsAndIcons { get; set; }
  public IFmiBindingCommon Binding { get; set; }

  public delegate void FmiLog(LogSeverity severity, string message);

  public event FmiLog? OnFmuLog;

  public FmuEntity(string fmuPath, FmiLog fmiLogCallback)
  {
    FmiVersion = ModelLoader.FindFmiVersion(fmuPath);
    OnFmuLog = fmiLogCallback;

    Binding = BindingFactory.CreateBinding(FmiVersion, fmuPath, RaiseOnFmuLogEvent);
    ModelDescription = Binding.ModelDescription;

    if (FmiVersion == FmiVersions.Fmi3)
    {
      // null if no TerminalsAndIcons
      TerminalsAndIcons = Binding.TerminalsAndIcons;
    }
    // Check loaded ModelDescription Co-Simulation attributes and verify its compatibility 
    ModelLoader.CheckCoSimAttributes(ModelDescription, TerminalsAndIcons, RaiseOnFmuLogEvent);
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

  public void PrepareFmu(Action fmuConfigurationAction, Action fmuInitializationAction)
  {
    switch (FmiVersion)
    {
      case FmiVersions.Fmi2:
        PrepareFmi2Fmu(fmuInitializationAction);
        break;
      case FmiVersions.Fmi3:
        PrepareFmi3Fmu(fmuConfigurationAction, fmuInitializationAction);
        break;
      default:
        throw new ArgumentOutOfRangeException("The provided FMI version is unknown.");
    }

    CurrentFmuSuperState = FmuSuperStates.Initialized;
  }

  private void PrepareFmi2Fmu(Action fmuInitializationAction)
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

    CurrentFmuSuperState = FmuSuperStates.Instantiated;

    fmi2Binding.SetDebugLogging(true, Array.Empty<string>());

    fmi2Binding.SetupExperiment(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    fmi2Binding.EnterInitializationMode();

    CurrentFmuSuperState = FmuSuperStates.Initializing;

    fmuInitializationAction();

    fmi2Binding.ExitInitializationMode();

    CurrentFmuSuperState = FmuSuperStates.Initialized;
  }

  private void PrepareFmi3Fmu(Action fmuConfigurationAction, Action fmuInitializationAction)
  {
    var fmi3Binding = (IFmi3Binding)Binding;
    // Get FMI Model binding

    fmi3Binding.InstantiateCoSimulation(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      true,
      true,
      ModelDescription.CoSimulation.hasEventMode,
      Fmi3Logger);

    CurrentFmuSuperState = FmuSuperStates.Instantiated;

    if (ModelDescription.Variables.Values.Any(v => v.Causality == Variable.Causalities.StructuralParameter))
    {
      // Enter configuration mode
      fmi3Binding.EnterConfigurationMode();

      fmuConfigurationAction();

      // Exit configuration mode
      fmi3Binding.ExitConfigurationMode();
    }

    foreach (var arrayVar in ModelDescription.ArrayVariables.Values)
    {
      arrayVar.InitializeArrayLength(ModelDescription.Variables);
    }

    fmi3Binding.SetDebugLogging(true, 0, null);

    fmi3Binding.EnterInitializationMode(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    CurrentFmuSuperState = FmuSuperStates.Initializing;

    fmuInitializationAction();

    fmi3Binding.ExitInitializationMode();


    CurrentFmuSuperState = FmuSuperStates.Initialized;
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

  /* Get terminal name with associated vRefIn (Rx_Data) and vRefOut (Tx_Data) */
  public Dictionary<string, (uint? /* vRefIn */, uint? /* vRefOut */)> GetTerminalsValueRefs()
  {
    var returnedDict = new Dictionary<string, (uint?, uint?)>();

    if (TerminalsAndIcons == null) 
    { 
      return returnedDict;
    }

    foreach (var (terminalName, terminal) in TerminalsAndIcons.Terminals)
    {
      uint? vRefIn = null;
      uint? vRefOut = null;
      foreach (var (_, variable) in terminal.TerminalMemberVariables)
      {
        var memberName = variable.MemberName;
        if (memberName == "Rx_Data")
        {
          vRefIn = variable.CorrespondingValueReference;
        }
        else if (memberName == "Tx_Data")
        {
          vRefOut = variable.CorrespondingValueReference;
        }
      }
      returnedDict[terminalName] = (vRefIn, vRefOut);
    }
    return returnedDict;
  }

  public double? GetStopTime()
  {
    return ModelDescription.DefaultExperiment?.StopTime;
  }

  public void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double _,
    out bool eventEncountered,
    out bool terminateRequested)
  {
    Binding.DoStep(currentCommunicationPoint, communicationStepSize, out _, out eventEncountered, out terminateRequested);
  }

  public void EnterEventMode()
  {
    switch (FmiVersion)
    {
      case FmiVersions.Fmi2:
        throw new ArgumentOutOfRangeException("TODO: Not implemented, supported or tested for FMI2.");        
      case FmiVersions.Fmi3:
        var fmi3Binding = (IFmi3Binding)Binding;
        fmi3Binding.EnterEventMode();
        break;
      default:
        throw new ArgumentOutOfRangeException("The provided FMI version is unknown.");
    }    
  }

  public void EnterStepMode()
  {
    switch (FmiVersion)
    {
      case FmiVersions.Fmi2:
        throw new ArgumentOutOfRangeException("TODO: Not implemented, supported or tested for FMI2.");        
      case FmiVersions.Fmi3:
        var fmi3Binding = (IFmi3Binding)Binding;
        fmi3Binding.EnterStepMode();
        break;
      default:
        throw new ArgumentOutOfRangeException("The provided FMI version is unknown.");
    }
  }



  public void UpdateDiscreteStates(out bool discreteStatesNeedUpdate, out bool terminateRequested)
  {
    switch (FmiVersion)
    {
      case FmiVersions.Fmi2:
        throw new ArgumentOutOfRangeException("TODO: Not implemented, supported or tested for FMI2.");        
      case FmiVersions.Fmi3:
        var fmi3Binding = (IFmi3Binding)Binding;
        fmi3Binding.UpdateDiscreteStates(out discreteStatesNeedUpdate, out terminateRequested);
        break;
      default:
        throw new ArgumentOutOfRangeException("The provided FMI version is unknown.");
    }
  }

  public void Terminate()
  {
    CurrentFmuSuperState = FmuSuperStates.Terminated;
    Binding.Terminate();
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
