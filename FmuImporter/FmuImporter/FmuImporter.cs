// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.Exceptions;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using FmuImporter.Exceptions;
using FmuImporter.SilKit;
using SilKit.Services.Logger;
using SilKit.Services.PubSub;

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

  private enum FmuSuperStates
  {
    Uninitialized,
    Initialized,
    Exited
  }

  private SilKitStatus CurrentSilKitStatus { get; set; } = SilKitStatus.Uninitialized;
  private FmuSuperStates CurrentFmuSuperState { get; set; } = FmuSuperStates.Uninitialized;

  private ModelDescription ModelDescription { get; set; } = null!;
  private IFmiBindingCommon Binding { get; set; } = null!;

  private SilKitManager SilKitManager { get; }
  private ConfiguredVariableManager? ConfiguredVariableManager { get; set; }

  private SortedList<ulong, Dictionary<uint, byte[]>> DataBuffer { get; }

  private readonly bool _useStopTime;

  private readonly Config.Configuration _fmuImporterConfig;

  private Dictionary<string, Config.Parameter>? _configuredParameters;

  private void LogCallback(LogLevel logLevel, string message)
  {
    SilKitManager.Logger.Log(logLevel, message);
  }

  private void InternalCallback(Fmi.Helpers.LogSeverity severity, string message)
  {
    LogCallback(Helpers.FmiLogLevelToSilKitLogLevel(severity), message);
  }

  public FmuImporter(
    string fmuPath,
    string? silKitConfigurationPath,
    string? fmuImporterConfigFilePath,
    string participantName,
    bool useStopTime)
  {
    SilKitManager = new SilKitManager(silKitConfigurationPath, participantName);
    CurrentSilKitStatus = SilKitStatus.Initialized;
    DataBuffer = new SortedList<ulong, Dictionary<uint, byte[]>>();

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

        _fmuImporterConfig.SetSilKitLogger(SilKitManager.Logger);

        _fmuImporterConfig.MergeIncludes();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }

    _useStopTime = useStopTime;
    try
    {
      InitializeFMU(fmuPath);
      PrepareConfiguredVariables();
    }
    catch (Exception e)
    {
      SilKitManager.Logger.Log(LogLevel.Error, e.ToString());
      ExitFmuImporter();
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
        SilKitManager.Dispose();

        // cleanup FMU
        Binding.Dispose();
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

  private void PrepareConfiguredVariables()
  {
    if (ConfiguredVariableManager == null)
    {
      throw new NullReferenceException($"{nameof(ConfiguredVariableManager)} was null.");
    }

    var configuredVariableDictionary =
      new Dictionary<uint, Config.ConfiguredVariable>(ModelDescription.Variables.Values.Count);

    var variableMappings = _fmuImporterConfig.GetVariables();

    for (var i = 0; i < variableMappings.Count; i++)
    {
      var configuredVariable = variableMappings.ElementAt(i).Value;
      if (configuredVariable.VariableName == null)
      {
        //TODO: this is redundant with Configuration.cs:UpdateVariablesDictionary:12
        throw new InvalidConfigurationException(
          $"The configured variable at index '{i}' does not have a variable name.");
      }

      var success =
        ModelDescription.NameToValueReference.TryGetValue(configuredVariable.VariableName, out var refValue);
      if (!success)
      {
        throw new InvalidConfigurationException(
          $"The configured variable '{configuredVariable.VariableName}' cannot be found in the model description.");
      }

      configuredVariableDictionary.Add(refValue, configuredVariable);
    }

    foreach (var modelDescriptionVariable in ModelDescription.Variables.Values)
    {
      if (modelDescriptionVariable.Causality
          is Variable.Causalities.Input
          or Variable.Causalities.Output
          or Variable.Causalities.Parameter
          or Variable.Causalities.StructuralParameter)
      {
        var success =
          configuredVariableDictionary.TryGetValue(modelDescriptionVariable.ValueReference, out var configuredVariable);
        if (!success)
        {
          // Only subscribe and publish unmapped variables if they are not ignored
          if (_fmuImporterConfig.IgnoreUnmappedVariables.GetValueOrDefault(false))
          {
            continue;
          }

          // initialize a default configured variable
          configuredVariable = new Config.ConfiguredVariable
          {
            VariableName = modelDescriptionVariable.Name,
            TopicName = modelDescriptionVariable.Name // default
          };
          configuredVariableDictionary.Add(modelDescriptionVariable.ValueReference, configuredVariable);
        }

        if (configuredVariable == null)
        {
          throw new NullReferenceException("The retrieved configured variable was null.");
        }

        configuredVariable.FmuVariableDefinition = modelDescriptionVariable;

        if (string.IsNullOrEmpty(configuredVariable.TopicName))
        {
          configuredVariable.TopicName = modelDescriptionVariable.Name;
        }

        if (configuredVariable.VariableName == null)
        {
          throw new NullReferenceException($"{nameof(configuredVariable.VariableName)} was null.");
        }

        if (configuredVariable.FmuVariableDefinition.Causality == Variable.Causalities.Input)
        {
          var sub = SilKitManager.CreateSubscriber(
            configuredVariable.VariableName,
            configuredVariable.TopicName,
            new IntPtr(configuredVariable.FmuVariableDefinition.ValueReference),
            DataMessageHandler);

          ConfiguredVariableManager.AddSubscriber(configuredVariable, sub);
        }
        else if (configuredVariable.FmuVariableDefinition.Causality == Variable.Causalities.Output)
        {
          var pub = SilKitManager.CreatePublisher(configuredVariable.VariableName, configuredVariable.TopicName, 0);
          ConfiguredVariableManager.AddPublisher(configuredVariable, pub);
        }
        else if (configuredVariable.FmuVariableDefinition.Causality is Variable.Causalities.Parameter
                 or Variable.Causalities.StructuralParameter)
        {
          var pub = SilKitManager.CreatePublisher(configuredVariable.VariableName, configuredVariable.TopicName, 0);
          ConfiguredVariableManager.AddPublisher(configuredVariable, pub);
        }
        // ignore variables with other causalities, such as calculatedParameter or local
      }
    }
  }

  private void DataMessageHandler(IntPtr context, IDataSubscriber subscriber, DataMessageEvent dataMessageEvent)
  {
    // buffer data
    // Use a last-is-best approach for storage

    var valueRef = (uint)context;

    // data is processed in sim. step callback (SimulationStepReached)
    if (DataBuffer.TryGetValue(dataMessageEvent.TimestampInNS, out var futureDict))
    {
      futureDict[valueRef] = dataMessageEvent.Data;
    }
    else
    {
      var dict = new Dictionary<uint, byte[]>
      {
        { valueRef, dataMessageEvent.Data }
      };
      DataBuffer.Add(dataMessageEvent.TimestampInNS, dict);
    }
  }

  private void InitializeFMU(string fmuPath)
  {
    _configuredParameters = _fmuImporterConfig.GetParameters();
    var fmiVersion = ModelLoader.FindFmiVersion(fmuPath);

    Fmi.Helpers.SetLoggerCallback(InternalCallback);

    switch (fmiVersion)
    {
      case FmiVersions.Fmi2:
        PrepareFmi2Fmu(fmuPath);
        break;
      case FmiVersions.Fmi3:
        PrepareFmi3Fmu(fmuPath);
        break;
      case FmiVersions.Invalid:
      // fallthrough
      default:
        throw new ModelDescriptionException("FMU did not provide a supported FMI version.");
    }

    CurrentFmuSuperState = FmuSuperStates.Initialized;
  }

  public void Fmi2Logger(
    string instanceName,
    FmiStatus status,
    string category,
    string message)
  {
    var msg = $"Logger: Name={instanceName}; status={status}; category={category};\n  message={message}";
    switch (status)
    {
      case FmiStatus.OK:
      case FmiStatus.Pending:
        LogCallback(LogLevel.Info, msg);
        break;
      case FmiStatus.Discard:
      case FmiStatus.Warning:
        LogCallback(LogLevel.Warn, msg);
        break;
      case FmiStatus.Error:
        LogCallback(LogLevel.Error, msg);
        break;
      case FmiStatus.Fatal:
        LogCallback(LogLevel.Critical, msg);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(status), status, null);
    }
  }

  public void Fmi2StepFinished(FmiStatus status)
  {
    ((IFmi2Binding)Binding).NotifyAsyncDoStepReturned(status);
  }

  private Fmi2BindingCallbackFunctions fmi2Functions;

  private void PrepareFmi2Fmu(string fmuPath)
  {
    // Get FMI Model binding
    var fmi2Binding = Fmi2BindingFactory.CreateFmi2Binding(fmuPath);
    Binding = fmi2Binding;

    // Get FMI ModelDescription
    ModelDescription = fmi2Binding.GetModelDescription();

    // Initialize ConfiguredVariableManager
    ConfiguredVariableManager = new ConfiguredVariableManager(Binding, ModelDescription);

    // Prepare FMU
    fmi2Functions = new Fmi2BindingCallbackFunctions(Fmi2Logger, Fmi2StepFinished);

    fmi2Binding.Instantiate(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      fmi2Functions,
      true,
      true);

    fmi2Binding.SetDebugLogging(true, Array.Empty<string>());

    fmi2Binding.SetupExperiment(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    fmi2Binding.EnterInitializationMode();

    ApplyParameterConfiguration();

    fmi2Binding.ExitInitializationMode();
  }

  private Fmi3LogMessageCallback? _logger;

  private void PrepareFmi3Fmu(string fmuPath)
  {
    // Get FMI Model binding
    var fmi3Binding = Fmi3BindingFactory.CreateFmi3Binding(fmuPath);
    Binding = fmi3Binding;

    // Get FMI ModelDescription
    ModelDescription = fmi3Binding.GetModelDescription();

    // Initialize ConfiguredVariableManager
    ConfiguredVariableManager = new ConfiguredVariableManager(Binding, ModelDescription);

    _logger = Logger;

    fmi3Binding.InstantiateCoSimulation(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      true,
      true,
      _logger);

    fmi3Binding.SetDebugLogging(true, 0, null);

    fmi3Binding.EnterInitializationMode(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    // initialize all configured parameters
    ApplyParameterConfiguration();

    fmi3Binding.ExitInitializationMode();
    CurrentFmuSuperState = FmuSuperStates.Initialized;
  }

  private void ApplyParameterConfiguration()
  {
    // initialize all configured parameters
    if (_configuredParameters != null)
    {
      foreach (var configuredParameter in _configuredParameters.Values)
      {
        if (configuredParameter.VariableName == null)
        {
          throw new InvalidConfigurationException(
            "A configured parameter did not contain a variable name.");
        }

        if (configuredParameter.Value == null)
        {
          throw new InvalidConfigurationException(
            $"Configured parameter for '{configuredParameter.VariableName}' did not contain a value.");
        }

        var result = ModelDescription.NameToValueReference.TryGetValue(
          configuredParameter.VariableName,
          out var refValue);
        if (!result)
        {
          throw new InvalidConfigurationException(
            $"Configured parameter '{configuredParameter.VariableName}' not found in model description.");
        }

        result = ModelDescription.Variables.TryGetValue(refValue, out var v);
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
          if (Binding.GetFmiVersion() == FmiVersions.Fmi2)
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
          Binding.SetValue(refValue, data);
        }
        else
        {
          // binary type
          if (Binding.GetFmiVersion() == FmiVersions.Fmi2)
          {
            throw new NotSupportedException("FMI 2.0.x does not support the binary data type.");
          }

          Binding.SetValue(refValue, data, binSizes.ToArray());
        }
      }
    }
  }

  private void Logger(IntPtr instanceEnvironment, FmiStatus status, string category, string message)
  {
    var msg = $"Logger: FmuEnvironment={instanceEnvironment}; status={status}; category={category};" +
              $"\n  message={message}";

    switch (status)
    {
      case FmiStatus.OK:
        LogCallback(LogLevel.Info, msg);
        break;
      case FmiStatus.Warning:
      case FmiStatus.Discard:
        LogCallback(LogLevel.Warn, msg);
        break;
      case FmiStatus.Error:
        LogCallback(LogLevel.Error, msg);
        break;
      case FmiStatus.Fatal:
        LogCallback(LogLevel.Critical, msg);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(status), status, null);
    }
  }

  public void StartSimulation()
  {
    LogCallback(LogLevel.Info, "Starting Simulation.");
    ulong stepDuration;
    if (_fmuImporterConfig.StepSize != null)
    {
      stepDuration = _fmuImporterConfig.StepSize.Value;
    }
    else
    {
      if (ModelDescription.DefaultExperiment.StepSize.HasValue)
      {
        stepDuration = Helpers.FmiTimeToSilKitTime(ModelDescription.DefaultExperiment.StepSize.Value);
      }
      else if (ModelDescription.CoSimulation.FixedInternalStepSize.HasValue)
      {
        stepDuration = Helpers.FmiTimeToSilKitTime(ModelDescription.CoSimulation.FixedInternalStepSize.Value);
      }
      else
      {
        stepDuration = Helpers.DefaultSimStepDuration;
      }
    }

    try
    {
      SilKitManager.StartSimulation(SimulationStepReached, stepDuration);
      CurrentSilKitStatus = SilKitStatus.LifecycleStarted;

      SilKitManager.WaitForLifecycleToComplete();
      CurrentSilKitStatus = SilKitStatus.ShutDown;
    }
    finally
    {
      ExitFmuImporter();
      if (CurrentSilKitStatus != SilKitStatus.ShutDown)
      {
        SilKitManager.WaitForLifecycleToComplete();
      }
    }
  }

  private ulong? _lastSimStep;
  private ulong _nextSimStep;

  private void SimulationStepReached(ulong nowInNs, ulong durationInNs)
  {
    if (ConfiguredVariableManager == null)
    {
      throw new NullReferenceException($"{nameof(ConfiguredVariableManager)} was null.");
    }

    if (nowInNs == 0)
    {
      // skip initialization - it was done already.
      // However, publish all initial output variable values
      ConfiguredVariableManager.PublishInitialData();
      _lastSimStep = nowInNs;
      return;
    }

    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var valueUpdates = PrepareUpdateAndTrimBuffer(_lastSimStep ?? nowInNs - durationInNs, DataBuffer);

    // update FMU
    foreach (var valueUpdate in valueUpdates)
    {
      ConfiguredVariableManager.SetValue(valueUpdate.Key, valueUpdate.Value);
    }

    _lastSimStep = nowInNs;
    _nextSimStep = nowInNs + durationInNs;

    // Calculate simulation step
    var fmiNow = Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    try
    {
      Binding.DoStep(
        fmiNow,
        Helpers.SilKitTimeToFmiTime(durationInNs),
        out _);
    }
    catch (Exception)
    {
      ExitFmuImporter();
      return;
    }

    ConfiguredVariableManager.PublishOutputData(false);

    if (_useStopTime && ModelDescription.DefaultExperiment.StopTime.HasValue)
    {
      if (Helpers.SilKitTimeToFmiTime(nowInNs) >= ModelDescription.DefaultExperiment.StopTime)
      {
        // stop the SIL Kit simulation
        SilKitManager.StopSimulation("FMU stopTime reached.");
      }
    }
  }

  private Dictionary<uint, byte[]> PrepareUpdateAndTrimBuffer(
    ulong currentTime,
    SortedList<ulong, Dictionary<uint, byte[]>> dataBuffer)
  {
    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var removeList = new List<ulong>();
    var valueUpdates = new Dictionary<uint, byte[]>();
    foreach (var timeDataPair in dataBuffer)
    {
      if (timeDataPair.Key <= currentTime)
      {
        foreach (var dataBufferKvp in timeDataPair.Value)
        {
          valueUpdates[dataBufferKvp.Key] = dataBufferKvp.Value;
        }

        removeList.Add(timeDataPair.Key);
      }
      else
      {
        // no need to iterate future events
        break;
      }
    }

    // remove all processed entries from the buffer
    foreach (var _ in removeList)
    {
      DataBuffer.RemoveAt(0);
    }

    return valueUpdates;
  }

  public void ExitFmuImporter()
  {
    if (CurrentFmuSuperState == FmuSuperStates.Initialized)
    {
      Binding.Terminate();
      CurrentFmuSuperState = FmuSuperStates.Exited;
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
          SilKitManager.StopSimulation("FMU Importer is exiting.");
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
}
