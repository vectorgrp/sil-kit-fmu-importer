// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;
using Fmi;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using FmuImporter.Exceptions;
using FmuImporter.Fmu;
using FmuImporter.SilKit;
using SilKit.Services.Logger;
using SilKit.Services.Orchestration;

namespace FmuImporter;

public class FmuImporter
{
  private enum SilKitStatus
  {
    Uninitialized,
    Initialized, // Connected & Logger available
    LifecycleStarted,
    Stopped,
    ShutDown
  }

  private SilKitStatus CurrentSilKitStatus { get; set; } = SilKitStatus.Uninitialized;

  private SilKitEntity SilKitEntity { get; }
  private SilKitDataManager SilKitDataManager { get; }
  private FmuEntity FmuEntity { get; }
  private FmuDataManager? FmuDataManager { get; set; }

  private readonly Configuration _fmuImporterConfig;

  private readonly Dictionary<string, Parameter>? _configuredParameters;
  private readonly Dictionary<string, Parameter>? _configuredStructuralParameters;


  public FmuImporter(
    string fmuPath,
    string? silKitConfigurationPath,
    string? fmuImporterConfigFilePath,
    string participantName,
    LifecycleService.LifecycleConfiguration.Modes lifecycleMode,
    TimeSyncModes timeSyncMode)
  {
    AppDomain.CurrentDomain.UnhandledException +=
      (sender, e) =>
      {
        Environment.ExitCode = ((Exception)e.ExceptionObject).HResult;
      };

    SilKitEntity = new SilKitEntity(
      silKitConfigurationPath,
      participantName,
      lifecycleMode,
      timeSyncMode);
    SilKitDataManager = new SilKitDataManager(SilKitEntity);
    CurrentSilKitStatus = SilKitStatus.Initialized;

    try
    {
      if (string.IsNullOrEmpty(fmuImporterConfigFilePath))
      {
        _fmuImporterConfig = new Configuration();
        _fmuImporterConfig.MergeIncludes();
      }
      else
      {
        _fmuImporterConfig = ConfigParser.LoadConfiguration(fmuImporterConfigFilePath);
        _fmuImporterConfig.SetSilKitLogger(SilKitEntity.Logger);
        _fmuImporterConfig.MergeIncludes();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }

    FmuEntity = new FmuEntity(fmuPath);

    try
    {
      _configuredParameters = _fmuImporterConfig.GetParameters();
      _configuredStructuralParameters = new Dictionary<string, Parameter>();

      var myStructParams = FmuEntity.ModelDescription.Variables
                                    .Where(kvp => kvp.Value.Causality == Variable.Causalities.StructuralParameter)
                                    .Select(v => v.Value.Name);
      foreach (var myStructParam in myStructParams)
      {
        var success = _configuredParameters.Remove(myStructParam, out var param);
        if (!success)
        {
          continue;
        }

        _configuredStructuralParameters.Add(myStructParam, param!);
      }

      // Register logger callback
      FmuEntity.OnFmuLog += FmuEntity_OnFmuLog;

      // Initialize FMU
      FmuEntity.PrepareFmu(ApplyParameterConfiguration, ApplyParameterConfiguration);

      // Initialize FmuDataManager
      FmuDataManager = new FmuDataManager(FmuEntity.Binding, FmuEntity.ModelDescription);

      PrepareConfiguredVariables();
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.ToString());
      Environment.ExitCode = e.HResult;
      ExitFmuImporter();
    }
  }

  private void FmuEntity_OnFmuLog(LogSeverity severity, string message)
  {
    SilKitEntity.Logger.Log(Helpers.FmiLogLevelToSilKitLogLevel(severity), message);
  }

  private void ApplyParameterConfiguration()
  {
    Dictionary<string, Parameter>.ValueCollection usedConfiguredParameters;
    var isHandlingStructuredParameters = false;

    // initialize all configured parameters
    if (FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Instantiated &&
        _configuredStructuralParameters != null)
    {
      usedConfiguredParameters = _configuredStructuralParameters.Values;
      isHandlingStructuredParameters = true;
    }
    else if (FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Initializing && _configuredParameters != null)
    {
      usedConfiguredParameters = _configuredParameters.Values;
    }
    else
    {
      throw new Exception();
    }

    if (usedConfiguredParameters.Count == 0)
    {
      return;
    }

    foreach (var configuredParameter in usedConfiguredParameters)
    {
      var result = FmuEntity.ModelDescription.NameToValueReference.TryGetValue(
        configuredParameter.VariableName,
        out var refValue);
      if (!result)
      {
        throw new InvalidConfigurationException(
          $"Configured parameter '{configuredParameter.VariableName}' not found in model description.");
      }

      result = FmuEntity.ModelDescription.Variables.TryGetValue(refValue, out var v);
      if (!result || v == null)
      {
        throw new InvalidConfigurationException(
          $"Configured parameter '{configuredParameter.VariableName}' not found in model description.");
      }

      byte[] data;
      var binSizes = new List<int>();
      if (configuredParameter.Value is List<object> objectList)
      {
        // the parameter is an array
        if (FmuEntity.FmiVersion == FmiVersions.Fmi2)
        {
          throw new NotSupportedException("FMI 2.0.x does not support arrays.");
        }

        if (isHandlingStructuredParameters)
        {
          // modify model description to make sure that the array lengths can be calculated correctly
          v.Start = objectList.ToArray();
        }

        data = Fmi.Supplements.Serializer.Serialize(objectList, v.VariableType, ref binSizes);
      }
      else
      {
        if (isHandlingStructuredParameters)
        {
          // modify model description to make sure that the array lengths can be calculated correctly
          v.Start = new[] { configuredParameter.Value };
        }

        data = Fmi.Supplements.Serializer.Serialize(configuredParameter.Value, v.VariableType, ref binSizes);
      }

      if (v.VariableType != VariableTypes.Binary)
      {
        FmuEntity.Binding.SetValue(refValue, data);
      }
      else
      {
        // binary type
        if (FmuEntity.FmiVersion == FmiVersions.Fmi2)
        {
          throw new NotSupportedException("FMI 2.0.x does not support the binary data type.");
        }

        FmuEntity.Binding.SetValue(refValue, data, binSizes.ToArray());
      }
    }
  }

  private void PrepareConfiguredVariables()
  {
    if (FmuDataManager == null)
    {
      throw new NullReferenceException($"{nameof(FmuDataManager)} was null.");
    }

    var relevantConfiguredVariables = FmuDataManager.Initialize(_fmuImporterConfig);

    foreach (var configuredVariable in relevantConfiguredVariables)
    {
      if (configuredVariable.FmuVariableDefinition.Causality == Variable.Causalities.Input)
      {
        SilKitDataManager.CreateSubscriber(
          configuredVariable.FmuVariableDefinition.Name,
          configuredVariable.TopicName,
          new IntPtr(configuredVariable.FmuVariableDefinition.ValueReference));
      }
      else if (configuredVariable.FmuVariableDefinition.Causality
               is Variable.Causalities.Output
               or Variable.Causalities.Parameter
               or Variable.Causalities.StructuralParameter
              )
      {
        SilKitDataManager.CreatePublisher(
          configuredVariable.FmuVariableDefinition.Name,
          configuredVariable.TopicName,
          new IntPtr(configuredVariable.FmuVariableDefinition.ValueReference),
          0);
      }
    }
  }

  public void StartSimulation()
  {
    SilKitEntity.Logger.Log(LogLevel.Info, "Starting Simulation.");
    ulong stepDuration;
    if (_fmuImporterConfig.StepSize != null)
    {
      stepDuration = _fmuImporterConfig.StepSize.Value;
    }
    else
    {
      var fmuStepSize = FmuEntity.GetStepSize();
      stepDuration = fmuStepSize.HasValue
                       ? Helpers.FmiTimeToSilKitTime(fmuStepSize.Value)
                       : Helpers.DefaultSimStepDuration;
    }

    try
    {
      SilKitEntity.StartSimulation(SimulationStepReached, stepDuration);
      CurrentSilKitStatus = SilKitStatus.LifecycleStarted;

      SilKitEntity.WaitForLifecycleToComplete();
      CurrentSilKitStatus = SilKitStatus.ShutDown;
    }
    catch (Exception e)
    {
      Environment.ExitCode = e.HResult;
    }
    finally
    {
      ExitFmuImporter();
      if (CurrentSilKitStatus != SilKitStatus.ShutDown)
      {
        SilKitEntity.WaitForLifecycleToComplete();
      }
    }
  }

  private ulong? _initialSimTime;
  private ulong? _lastSimStep;
  private Stopwatch? _stopWatch;
  private long _nextSimTime;

  private void SimulationStepReached(ulong nowInNs, ulong durationInNs)
  {
    if (FmuDataManager == null)
    {
      throw new NullReferenceException($"{nameof(FmuDataManager)} was null.");
    }

    if (FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Exited)
    {
      ExitFmuImporter();
      return;
    }

    // wall-clock alignment init
    if (SilKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      long elapsedTime;
      if (_stopWatch == null)
      {
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
        elapsedTime = 0;
      }
      else
      {
        elapsedTime = _stopWatch.ElapsedMilliseconds;
      }

      // in ms
      var deltaToNextSimStep = _nextSimTime - (elapsedTime * 1_000_000);
      // TODO improve timer performance to decrease threshold (ms is a lot)
      if (deltaToNextSimStep > 2 * 1_000_000 /* 2 ms in ns */)
      {
        new ManualResetEvent(false).WaitOne((int)(deltaToNextSimStep / 1_000_000));
      }

      if (IsSimulationStopped())
      {
        return;
      }

      _nextSimTime += (long)durationInNs;
    }

    if (_initialSimTime == null)
    {
      _lastSimStep = nowInNs;
      _initialSimTime = nowInNs;
      // skip initialization - it was done already.
      // However, publish all initial output variable values
      var initialData = FmuDataManager.GetInitialData();
      SilKitDataManager.PublishAll(initialData);
      return;
    }

    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var receivedSilKitData = SilKitDataManager.RetrieveReceivedData(_lastSimStep!.Value);
    FmuDataManager.SetData(receivedSilKitData);

    _lastSimStep = nowInNs;

    if (_initialSimTime > 0)
    {
      // apply offset to initial time in hop-on scenario
      nowInNs -= _initialSimTime.Value;
    }

    // Calculate simulation step
    var fmiNow = Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    try
    {
      FmuEntity.DoStep(
        fmiNow,
        Helpers.SilKitTimeToFmiTime(durationInNs));
    }
    catch (Exception e)
    {
      ExitFmuImporter();
      Environment.ExitCode = e.HResult;
      return;
    }

    var currentOutputData = FmuDataManager.GetOutputData(false);
    SilKitDataManager.PublishAll(currentOutputData);

    var stopTime = FmuEntity.GetStopTime();
    if (stopTime.HasValue)
    {
      if (Helpers.SilKitTimeToFmiTime(nowInNs) >= stopTime)
      {
        // stop the SIL Kit simulation
        SilKitEntity.StopSimulation("FMU stopTime reached.");
        _stopWatch?.Stop();
        _stopWatch = null;
      }
    }
  }

  private bool IsSimulationStopped()
  {
    return FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Exited ||
           CurrentSilKitStatus == SilKitStatus.Stopped ||
           CurrentSilKitStatus == SilKitStatus.ShutDown;
  }

  public void ExitFmuImporter()
  {
    if (FmuEntity.CurrentFmuSuperState == FmuEntity.FmuSuperStates.Initialized)
    {
      FmuEntity.Terminate();
      // FreeInstance will be called by the dispose pattern
    }

    switch (CurrentSilKitStatus)
    {
      case SilKitStatus.Uninitialized:
      case SilKitStatus.Initialized:
        // NOP - SIL Kit did not start a lifecycle yet
        break;
      case SilKitStatus.LifecycleStarted:
        try
        {
          SilKitEntity.StopSimulation("FMU Importer is exiting.");
        }
        finally
        {
          CurrentSilKitStatus = SilKitStatus.Stopped;
        }

        break;
      case SilKitStatus.Stopped:
      case SilKitStatus.ShutDown:
        // NOP - SIL Kit already stopped
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

#region IDisposable

  ~FmuImporter()
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

        // cleanup SIL Kit
        SilKitDataManager.Dispose();
        SilKitEntity.Dispose();

        // cleanup FMU
        FmuEntity.Dispose();
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
