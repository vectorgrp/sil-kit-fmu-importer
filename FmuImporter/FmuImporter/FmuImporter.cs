using Fmi;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using FmuImporter.SilKit;
using SilKit.Services.Logger;
using SilKit.Services.PubSub;

namespace FmuImporter;

public class FmuImporter
{
  private ModelDescription ModelDescription { get; set; } = null!;
  private IFmiBindingCommon Binding { get; set; } = null!;

  private SilKitManager SilKitManager { get; }
  private ConfiguredVariableManager? ConfiguredVariableManager { get; set; }

  private Dictionary<uint, byte[]> DataBuffer { get; } = new Dictionary<uint, byte[]>();
  private Dictionary<uint, byte[]> FutureDataBuffer { get; } = new Dictionary<uint, byte[]>();

  private readonly bool _useStopTime;

  private readonly Config.Configuration _fmuImporterConfig;

  private Dictionary<string, Config.Parameter>? _configuredParameters;

  private void LogCallback(LogLevel logLevel, string message)
  {
    SilKitManager.Logger.Log(logLevel, message);
  }

  public FmuImporter(string fmuPath, string? silKitConfigurationPath, string? fmuImporterConfigFilePath, string participantName, bool useStopTime)
  {
    SilKitManager = new SilKitManager(silKitConfigurationPath, participantName);

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
          throw new BadImageFormatException("The provided configuration file did not contain a valid 'Version' field.");
        }
        _fmuImporterConfig.MergeIncludes();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }

    _useStopTime = useStopTime;

    InitializeFMU(fmuPath);
    PrepareConfiguredVariables();
  }

  #region IDisposable

  ~FmuImporter()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool mDisposedValue;

  protected virtual void Dispose(bool disposing)
  {
    if (!mDisposedValue)
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
      mDisposedValue = true;
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
      // TODO throw?
      return;
    }

    var configuredVariableDictionary =
      new Dictionary<uint, Config.ConfiguredVariable>(ModelDescription.Variables.Values.Count);

    if (_fmuImporterConfig.VariableConfiguration?.Mappings != null)
    {
      for (var i = 0; i < _fmuImporterConfig.VariableConfiguration.Mappings.Count; i++)
      {
        var configuredVariable = _fmuImporterConfig.VariableConfiguration.Mappings[i];
        if (configuredVariable.VariableName == null)
        {
          throw new BadImageFormatException($"The configured variable at index '{i}' does not have a variable name.");
        }

        var success =
          ModelDescription.NameToValueReference.TryGetValue(configuredVariable.VariableName, out var refValue);
        if (!success)
        {
          throw new BadImageFormatException($"The configured variable '{configuredVariable.VariableName}' cannot be found in the model description.");
        }

        configuredVariableDictionary.Add(refValue, configuredVariable);
      }
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
          // TODO elaborate
          throw new NullReferenceException();
        }

        if (configuredVariable.FmuVariableDefinition.Causality == Variable.Causalities.Input)
        {
          var sub = SilKitManager.CreateSubscriber(configuredVariable.VariableName, configuredVariable.TopicName,
            new IntPtr(configuredVariable.FmuVariableDefinition.ValueReference), DataMessageHandler);

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

    if (dataMessageEvent.timestampInNs > nextSimStep)
    {
      throw new InvalidDataException(
        "The received message is further in the future than the next communication step!");
    }

    if (dataMessageEvent.timestampInNs > lastSimStep)
    {
      // data must not be processed in next SimStep
      if (!FutureDataBuffer.ContainsKey(valueRef))
      {
        FutureDataBuffer.TryAdd(valueRef, dataMessageEvent.data);
      }
      else
      {
        FutureDataBuffer[valueRef] = dataMessageEvent.data;
      }
    }
    else
    {
      if (!DataBuffer.ContainsKey(valueRef))
      {
        DataBuffer.TryAdd(valueRef, dataMessageEvent.data);
      }
      else
      {
        DataBuffer[valueRef] = dataMessageEvent.data;
      }
    }
  }

  private void InitializeFMU(string fmuPath)
  {
    _configuredParameters = _fmuImporterConfig.GetParameters();
    var fmiVersion = ModelLoader.FindFmiVersion(fmuPath);
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
        throw new ArgumentException("fmu did not provide a supported FMI version.");
    }
  }

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
    var functions = new Fmi2BindingCallbackFunctions(
      loggerDelegate: (name, status, category, message) =>
      {
        string msg = $"Logger: Name={name}; status={status}; category={category};\n  message={message}";
        switch (status)
        {
          case Fmi2Statuses.OK:
          case Fmi2Statuses.Pending:
            LogCallback(LogLevel.Info, msg);
            break;
          case Fmi2Statuses.Discard:
          case Fmi2Statuses.Warning:
            LogCallback(LogLevel.Warn, msg);
            break;
          case Fmi2Statuses.Error:
            LogCallback(LogLevel.Error, msg);
            break;
          case Fmi2Statuses.Fatal:
            LogCallback(LogLevel.Critical, msg);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

      },
      stepFinishedDelegate: status =>
      {
        fmi2Binding.NotifyAsyncDoStepReturned(status);
      });

    fmi2Binding.Instantiate(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      functions,
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

  private void PrepareFmi3Fmu(string fmuPath)
  {
    // Get FMI Model binding
    var fmi3Binding = Fmi3BindingFactory.CreateFmi3Binding(fmuPath);
    Binding = fmi3Binding;

    // Get FMI ModelDescription
    ModelDescription = fmi3Binding.GetModelDescription();

    // Initialize ConfiguredVariableManager
    ConfiguredVariableManager = new ConfiguredVariableManager(Binding, ModelDescription);

    fmi3Binding.InstantiateCoSimulation(
      ModelDescription.ModelName,
      ModelDescription.InstantiationToken,
      true,
      true,
      Logger);

    fmi3Binding.SetDebugLogging(true, 0, null);

    fmi3Binding.EnterInitializationMode(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    // initialize all configured parameters
    ApplyParameterConfiguration();

    fmi3Binding.ExitInitializationMode();
  }

  private void ApplyParameterConfiguration()
  {
    // initialize all configured parameters
    if (_configuredParameters != null)
    {
      foreach (var configuredParameter in _configuredParameters.Values)
      {
        if (configuredParameter.Value == null)
        {
          throw new BadImageFormatException(
            $"configured parameter for '{configuredParameter.VarName}' did not contain a value.");
        }

        var result = ModelDescription.NameToValueReference.TryGetValue(configuredParameter.VarName, out var refValue);
        if (!result)
        {
          throw new ArgumentException(
            $"Configured parameter '{configuredParameter.VarName}' not found in model description.");
        }
        result = ModelDescription.Variables.TryGetValue(refValue, out var v);
        if (!result || v == null)
        {
          throw new ArgumentException(
            $"Configured parameter '{configuredParameter.VarName}' not found in model description.");
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
          data = Fmi.Helpers.EncodeData(objectList, v.VariableType, ref binSizes);
        }
        else
        {
          data = Fmi.Helpers.EncodeData(configuredParameter.Value, v.VariableType, ref binSizes);
        }

        if (v.VariableType != typeof(IntPtr))
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

  private void Logger(IntPtr instanceEnvironment, Fmi3Statuses status, string category, string message)
  {
    string msg = $"Logger: FmuEnvironment={instanceEnvironment}; status={status}; category={category};" +
                 $"\n  message={message}";

    switch (status)
    {
      case Fmi3Statuses.OK:
        LogCallback(LogLevel.Info, msg);
        break;
      case Fmi3Statuses.Warning:
      case Fmi3Statuses.Discard:
        LogCallback(LogLevel.Warn, msg);
        break;
      case Fmi3Statuses.Error:
        LogCallback(LogLevel.Error, msg);
        break;
      case Fmi3Statuses.Fatal:
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

    SilKitManager.StartSimulation(SimulationStepReached, stepDuration);
  }

  private ulong? lastSimStep;
  private ulong nextSimStep;

  private void SimulationStepReached(ulong nowInNs, ulong durationInNs)
  {
    if (ConfiguredVariableManager == null)
    {
      // TODO throw, return or fix this code in general
      throw new Exception();
    }

    lastSimStep = nowInNs;
    nextSimStep = nowInNs + durationInNs;

    if (nowInNs == 0)
    {
      // skip initialization - it was done already.
      // However, publish all initial output variable values
      ConfiguredVariableManager.PublishInitialData();
      return;
    }

    foreach (var dataBufferKvp in DataBuffer)
    {
      // apply reverse transformation is necessary
      ConfiguredVariableManager.SetValue(dataBufferKvp.Key, dataBufferKvp.Value);
      //Binding.SetValue(dataBufferKvp.Key, dataBufferKvp.Value);
    }

    DataBuffer.Clear();

    // Calculate simulation step
    var fmiNow = Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    Binding.DoStep(
      fmiNow,
      Helpers.SilKitTimeToFmiTime(durationInNs),
      out _);

    ConfiguredVariableManager.PublishOutputData(false);

    if (_useStopTime && ModelDescription.DefaultExperiment.StopTime.HasValue)
    {
      if (fmiNow >= ModelDescription.DefaultExperiment.StopTime)
      {
        // stop the SIL Kit simulation
        SilKitManager.StopSimulation("FMU stopTime reached.");
        Binding.Terminate();
        return;
      }
    }

    // now that the current time step was processed completely, move 'future' events to current events
    foreach (var kvp in FutureDataBuffer)
    {
      DataBuffer.Add(kvp.Key, kvp.Value);
    }

    FutureDataBuffer.Clear();
  }
}
