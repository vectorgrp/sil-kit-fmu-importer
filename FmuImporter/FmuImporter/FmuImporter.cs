using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using SilKit;
using SilKit.Config;
using SilKit.Services.Logger;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;
using SilKit.Supplements.VendorData;

namespace FmuImporter;

public class FmuImporter
{
  internal class SilKitServices
  {
    public SilKitServices(
      Participant participant,
      ILifecycleService lifecycleService,
      LifecycleService.ITimeSyncService timeSyncService)
    {
      Participant = participant;
      LifecycleService = lifecycleService;
      TimeSyncService = timeSyncService;
      ValueRefToDataPublisher = new Dictionary<uint, IDataPublisher>();
      ValueRefToDataSubscriber = new Dictionary<uint, IDataSubscriber>();
    }

    public Participant Participant { get; set; }
    public ILifecycleService LifecycleService { get; set; }
    public LifecycleService.ITimeSyncService TimeSyncService { get; set; }
    public static ILogger? SilKitLogger { get; set; }

    public Dictionary<uint /* ref */, IDataPublisher> ValueRefToDataPublisher { get; set; }
    public Dictionary<uint /* ref */, IDataSubscriber> ValueRefToDataSubscriber { get; set; }
  }


  private ModelDescription ModelDescription { get; set; } = null!;
  private IFmiBindingCommon Binding { get; set; } = null!;

  private SilKitServices? silKitInstance;
  internal SilKitServices SilKitInstance {
    get {
      if ( silKitInstance == null )
      {
        throw new NullReferenceException("SilKit was not initialized properly.");
      }
      return silKitInstance;
    }
  }
  private readonly Dictionary<Type, List<uint>> outputValueReferencesByType;

  private Dictionary<uint, byte[]> DataBuffer { get; }
  private Dictionary<uint, byte[]> FutureDataBuffer { get; }

  private readonly bool ignoreStopTime;

  private static void LogCallback(LogLevel logLevel, string message)
  {
    if (SilKitServices.SilKitLogger == null)
    {
      Debug.WriteLine($"Dropped log message due to unavailable SIL Kit logger. Original message: '{message}'");
    }
    else
    {
      SilKitServices.SilKitLogger.Log(logLevel, message);
    }
  }

  public FmuImporter(string fmuPath, string silKitConfigurationPath, string participantName, bool ignoreStopTime)
  {
    // Initialize SIL Kit first to allow logging
    InitializeSilKit(silKitConfigurationPath, participantName);

    this.ignoreStopTime = ignoreStopTime;

    outputValueReferencesByType = new Dictionary<Type, List<uint>>()
    {
      { typeof(float), new List<uint>() },
      { typeof(double), new List<uint>() },
      { typeof(sbyte), new List<uint>() },
      { typeof(byte), new List<uint>() },
      { typeof(short), new List<uint>() },
      { typeof(ushort), new List<uint>() },
      { typeof(int), new List<uint>() },
      { typeof(uint), new List<uint>() },
      { typeof(long), new List<uint>() },
      { typeof(ulong), new List<uint>() },
      { typeof(bool), new List<uint>() },
      { typeof(string), new List<uint>() },
      { typeof(IntPtr), new List<uint>() }
    };

    DataBuffer = new Dictionary<uint, byte[]>();
    FutureDataBuffer = new Dictionary<uint, byte[]>();

    InitializeFMU(fmuPath);
    PrepareVariableDistribution();

    ConfigureSilKitServices();
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
        SilKitInstance.Participant.Dispose();

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

  private void PrepareVariableDistribution()
  {
    foreach (var modelDescriptionVariable in ModelDescription.Variables.Values)
    {
      if (modelDescriptionVariable.Causality == Variable.Causalities.Input)
      {
        var dataSpec = new PubSubSpec(modelDescriptionVariable.Name, Vector.MediaTypeData);
        var sub = SilKitInstance.Participant.CreateDataSubscriber(
          modelDescriptionVariable.Name,
          dataSpec,
          new IntPtr(modelDescriptionVariable.ValueReference),
          DataMessageHandler);
        SilKitInstance.ValueRefToDataSubscriber.Add(modelDescriptionVariable.ValueReference, sub);
      }
      else if (modelDescriptionVariable.Causality == Variable.Causalities.Output)
      {
        var dataSpec = new PubSubSpec(modelDescriptionVariable.Name, Vector.MediaTypeData);
        var pub = SilKitInstance.Participant.CreateDataPublisher(
          modelDescriptionVariable.Name,
          dataSpec, 0);
        SilKitInstance.ValueRefToDataPublisher.Add(modelDescriptionVariable.ValueReference, pub);

        if (!outputValueReferencesByType.TryGetValue(modelDescriptionVariable.VariableType, out var list))
        {
          throw new NotSupportedException("The detected FMI variable type is unknown");
        }

        list.Add(modelDescriptionVariable.ValueReference);
      }
      else if (modelDescriptionVariable.Causality == Variable.Causalities.Parameter)
      {
      }
      else
      {
      }
    }
  }


  private void DataMessageHandler(IntPtr context, IDataSubscriber subscriber, DataMessageEvent dataMessageEvent)
  {
    var valueRef = (uint)context;

    // buffer data
    // Currently, we use a last-is-best approach - this may be improved in the future

    if (dataMessageEvent.timestampInNs > nextSimStep)
    {
      throw new InvalidDataException(
        "The received message is further in the future than the next communication step!");
    }
    else if (dataMessageEvent.timestampInNs > lastSimStep)
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
    var fmiVersion = ModelLoader.FindFmiVersion(fmuPath);
    switch (fmiVersion)
    {
      case ModelLoader.FmiVersions.Fmi2:
        PrepareFmi2Fmu(fmuPath);
        break;
      case ModelLoader.FmiVersions.Fmi3:
        PrepareFmi3Fmu(fmuPath);
        break;
      case ModelLoader.FmiVersions.Invalid:
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
    fmi2Binding.ExitInitializationMode();
  }

  private void PrepareFmi3Fmu(string fmuPath)
  {
    // Get FMI Model binding
    var fmi3Binding = Fmi3BindingFactory.CreateFmi3Binding(fmuPath);
    Binding = fmi3Binding;
    // Get FMI ModelDescription
    ModelDescription = fmi3Binding.GetModelDescription();

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

    // initialize all 'exact' and 'approx' values
    fmi3Binding.ExitInitializationMode();
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

  private void InitializeSilKit(string silKitConfigurationPath, string participantName)
  {
    if (string.IsNullOrEmpty(participantName))
    {
      participantName = ModelDescription.ModelName;
    }

    var wrapper = SilKitWrapper.Instance;
    ParticipantConfiguration config;
    if (string.IsNullOrEmpty(silKitConfigurationPath))
    {
      config = wrapper.GetConfigurationFromString("");
    }
    else
    {
      config = wrapper.GetConfigurationFromFile(silKitConfigurationPath);
    }

    var lc = new LifecycleService.LifecycleConfiguration(LifecycleService.LifecycleConfiguration.Modes.Coordinated);

    var participant = wrapper.CreateParticipant(config, participantName);
    var lcs = participant.CreateLifecycleService(lc);
    var tss = lcs.CreateTimeSyncService();

    silKitInstance = new SilKitServices(participant, lcs, tss);
    // get logger
    SilKitServices.SilKitLogger = SilKitInstance.Participant.GetLogger();
    config.Dispose();
  }

  private void ConfigureSilKitServices()
  {
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

    SilKitInstance.TimeSyncService.SetSimulationStepHandler(SimulationStepReached, stepDuration);
  }

  private ulong? lastSimStep = null;
  private ulong nextSimStep = 0L;

  private void SimulationStepReached(ulong nowInNs, ulong durationInNs)
  {
    lastSimStep = nowInNs;
    nextSimStep = nowInNs + durationInNs;

    if (nowInNs == 0)
    {
      // skip initialization - it was done already.
      // However, publish all initial output variable values
      PublishAllOutputData();
      return;
    }

    foreach (var dataBufferKvp in DataBuffer)
    {
      Binding.SetValue(dataBufferKvp.Key, dataBufferKvp.Value);
    }

    DataBuffer.Clear();

    // Calculate simulation step
    var fmiNow = Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    Binding.DoStep(
      fmiNow,
      Helpers.SilKitTimeToFmiTime(durationInNs),
      out _);

    PublishAllOutputData();

    if (!ignoreStopTime && ModelDescription.DefaultExperiment.StopTime.HasValue)
    {
      if (fmiNow >= ModelDescription.DefaultExperiment.StopTime)
      {
        // stop the SIL Kit simulation
        SilKitInstance.LifecycleService.Stop("FMU stopTime reached.");
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

  private void PublishAllOutputData()
  {
    foreach (var varKvp in outputValueReferencesByType)
    {
      if (varKvp.Value.Count == 0)
      {
        continue;
      }

      var valueRefArr = varKvp.Value.ToArray();
      if (varKvp.Key == typeof(float))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<float> result);

        // Apply unit transformation
        // SIL Kit value = ([FMU value] / factor) - offset
        foreach (var variable in result.ResultArray)
        {
          var mdVar = ModelDescription.Variables[variable.ValueReference];
          for (int i = 0; i < variable.Values.Length; i++)
          {
            var unit = mdVar.TypeDefinition?.Unit;
            if (unit != null)
            {
              var value = variable.Values[i];
              // first reverse offset...
              if (unit.Offset.HasValue)
              {
                value = Convert.ToSingle(value - unit.Offset.Value);
              }

              // ...then reverse factor
              if (unit.Factor.HasValue)
              {
                value = Convert.ToSingle(value / unit.Factor.Value);
              }

              variable.Values[i] = value;
            }
          }
        }

        PublishData(result);
      }
      else if (varKvp.Key == typeof(double))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<double> result);

        // Apply unit transformation
        // SIL Kit value = ([FMU value] / factor) - offset
        foreach (var variable in result.ResultArray)
        {
          var mdVar = ModelDescription.Variables[variable.ValueReference];
          for (int i = 0; i < variable.Values.Length; i++)
          {
            var unit = mdVar.TypeDefinition?.Unit;
            if (unit != null)
            {
              var value = variable.Values[i];
              // first reverse offset...
              if (unit.Offset.HasValue)
              {
                value -= unit.Offset.Value;
              }

              // ...then reverse factor
              if (unit.Factor.HasValue)
              {
                value /= unit.Factor.Value;
              }

              variable.Values[i] = value;
            }
          }
        }

        PublishData(result);
      }
      else if (varKvp.Key == typeof(sbyte))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<sbyte> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(byte))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<byte> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(short))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<short> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(ushort))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<ushort> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(int))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<int> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(uint))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<uint> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(long))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<long> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(ulong))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<ulong> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(bool))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<bool> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(string))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<string> result);
        PublishData(result);
      }
      else if (varKvp.Key == typeof(IntPtr))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<IntPtr> result);
        PublishData(result);
      }
    }
  }

  private void PublishData(ReturnVariable<float> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<double> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<sbyte> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<byte> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<short> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<ushort> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<int> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<uint> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<long> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<ulong> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<bool> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<string> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = InitByteList(variable.IsScalar, variable.Values.Length);
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        byteList.AddRange(arrayPrefix);
      }

      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = Encoding.ASCII.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }

      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<IntPtr> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var publishableBytes = InitByteList(variable.IsScalar, variable.Values.Length);
      var arraySize = variable.ValueSizes.Length;

      for (int i = 0; i < arraySize; i++)
      {
        var binarySize = (int)variable.ValueSizes[i];
        publishableBytes.AddRange(BitConverter.GetBytes(arraySize));
        var targetByteArr = new byte[binarySize];
        var binaryPtr = variable.Values[i];
        Marshal.Copy(binaryPtr, targetByteArr, 0, binarySize);
        publishableBytes.AddRange(targetByteArr);
      }

      // publish byte array
      SilKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(publishableBytes.ToArray());
    }
  }

  private static List<byte> InitByteList(bool isScalar, int arrayLength)
  {
    if (isScalar)
    {
      return new List<byte>();
    }
    else
    {
      var byteArr = BitConverter.GetBytes(arrayLength);
      FormatData(ref byteArr);

      return byteArr.ToList();
    }
  }

  private static void FormatData(ref byte[] bytes)
  {
    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
  }

  public void RunSimulation()
  {
    SilKitInstance.LifecycleService.StartLifecycle();
    SilKitInstance.LifecycleService.WaitForLifecycleToComplete();
  }
}
