// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.FmiModel.Internal;
using FmuImporter.Exceptions;
using FmuImporter.Fmu;
using FmuImporter.SilKit;
using SilKit.Services.Logger;

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


  private readonly bool _useStopTime;

  private readonly Config.Configuration _fmuImporterConfig;

  private readonly Dictionary<string, Config.Parameter>? _configuredParameters;


  public FmuImporter(
    string fmuPath,
    string? silKitConfigurationPath,
    string? fmuImporterConfigFilePath,
    string participantName,
    bool useStopTime)
  {
    SilKitEntity = new SilKitEntity(silKitConfigurationPath, participantName);
    SilKitDataManager = new SilKitDataManager(SilKitEntity);
    CurrentSilKitStatus = SilKitStatus.Initialized;

    try
    {
      if (string.IsNullOrEmpty(fmuImporterConfigFilePath))
      {
        _fmuImporterConfig = new Config.Configuration();
        _fmuImporterConfig.MergeIncludes();
      }
      else
      {
        _fmuImporterConfig = Config.ConfigParser.LoadConfiguration(fmuImporterConfigFilePath);
        if (_fmuImporterConfig.Version == null || _fmuImporterConfig.Version == 0)
        {
          throw new InvalidConfigurationException(
            "The provided configuration file did not contain a valid 'Version' field.");
        }

        _fmuImporterConfig.SetSilKitLogger(SilKitEntity.Logger);

        _fmuImporterConfig.MergeIncludes();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }

    _useStopTime = useStopTime;
    FmuEntity = new FmuEntity(fmuPath);
    try
    {
      // Initialize FMU
      _configuredParameters = _fmuImporterConfig.GetParameters();
      FmuEntity.PrepareFmu(ApplyParameterConfiguration);
      FmuEntity.OnFmuLog += FmuEntity_OnFmuLog;

      // Initialize FmuDataManager
      FmuDataManager = new FmuDataManager(FmuEntity.Binding, FmuEntity.ModelDescription);

      PrepareConfiguredVariables();
    }
    catch (Exception e)
    {
      SilKitEntity.Logger.Log(LogLevel.Error, e.ToString());
      ExitFmuImporter();
    }
  }

  private void FmuEntity_OnFmuLog(LogSeverity severity, string message)
  {
    SilKitEntity.Logger.Log(Helpers.FmiLogLevelToSilKitLogLevel(severity), message);
  }

  private void ApplyParameterConfiguration()
  {
    // initialize all configured parameters
    if (_configuredParameters != null)
    {
      foreach (var configuredParameter in _configuredParameters.Values)
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

          data = Fmi.Supplements.Serializer.Serialize(objectList, v.VariableType, ref binSizes);
        }
        else
        {
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
          configuredVariable.ImporterVariableConfiguration.TopicName,
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
          configuredVariable.ImporterVariableConfiguration.TopicName,
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
    finally
    {
      ExitFmuImporter();
      if (CurrentSilKitStatus != SilKitStatus.ShutDown)
      {
        SilKitEntity.WaitForLifecycleToComplete();
      }
    }
  }

  private ulong? _lastSimStep;

  private void SimulationStepReached(ulong nowInNs, ulong durationInNs)
  {
    if (FmuDataManager == null)
    {
      throw new NullReferenceException($"{nameof(FmuDataManager)} was null.");
    }

    if (nowInNs == 0)
    {
      // skip initialization - it was done already.
      // However, publish all initial output variable values
      var initialData = FmuDataManager.GetInitialData();
      SilKitDataManager.PublishAll(initialData);
      _lastSimStep = nowInNs;
      return;
    }

    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var receivedSilKitData = SilKitDataManager.RetrieveReceivedData(_lastSimStep ?? nowInNs - durationInNs);
    FmuDataManager.SetData(receivedSilKitData);

    _lastSimStep = nowInNs;

    // Calculate simulation step
    var fmiNow = Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    try
    {
      FmuEntity.DoStep(
        fmiNow,
        Helpers.SilKitTimeToFmiTime(durationInNs));
    }
    catch (Exception)
    {
      ExitFmuImporter();
      return;
    }

    var currentOutputData = FmuDataManager.GetOutputData(false);
    SilKitDataManager.PublishAll(currentOutputData);

    var stopTime = FmuEntity.GetStopTime();
    if (_useStopTime && stopTime.HasValue)
    {
      if (Helpers.SilKitTimeToFmiTime(nowInNs) >= stopTime)
      {
        // stop the SIL Kit simulation
        SilKitEntity.StopSimulation("FMU stopTime reached.");
      }
    }
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
