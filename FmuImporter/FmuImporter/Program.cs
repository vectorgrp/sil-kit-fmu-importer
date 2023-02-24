using System.CommandLine;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using SilKit;
using SilKit.Config;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;
using SilKit.Supplements.VendorData;

namespace FmuImporter;

public class FmuImporter
{
  private struct SilKitInstance
  {
    public SilKitInstance()
    {
      Participant = null!;
      LifecycleService = null!;
      TimeSyncService = null!;
      ValueRefToDataPublisher = new Dictionary<uint, IDataPublisher>();
      ValueRefToDataSubscriber = new Dictionary<uint, IDataSubscriber>();
    }

    public Participant Participant { get; set; }
    public ILifecycleService LifecycleService { get; set; }
    public LifecycleService.ITimeSyncService TimeSyncService { get; set; }


    public Dictionary<uint /* ref */, IDataPublisher> ValueRefToDataPublisher { get; set; }
    public Dictionary<uint /* ref */, IDataSubscriber> ValueRefToDataSubscriber { get; set; }
  }


  private ModelDescription ModelDescription { get; set; } = null!;
  private IFmiBindingCommon Binding { get; set; } = null!;

  private SilKitInstance silKitInstance;
  private readonly Dictionary<Type, List<uint>> outputValueReferencesByType;

  private Dictionary<uint, byte[]> DataBuffer { get; set; }
  private Dictionary<uint, byte[]> FutureDataBuffer { get; set; }

  public FmuImporter(string fmuPath, string silKitConfigurationPath, string participantName)
  {
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
    InitializeSilKit(silKitConfigurationPath, participantName);
    PrepareVariableDistribution();
  }

  private void PrepareVariableDistribution()
  {
    foreach (var modelDescriptionVariable in ModelDescription.Variables.Values)
    {
      if (modelDescriptionVariable.Causality == Variable.Causalities.Input)
      {
        var dataSpec = new PubSubSpec(modelDescriptionVariable.Name, Vector.MediaTypeData);
        var sub = silKitInstance.Participant.CreateDataSubscriber(
          modelDescriptionVariable.Name,
          dataSpec,
          new IntPtr(modelDescriptionVariable.ValueReference),
          DataMessageHandler);
        silKitInstance.ValueRefToDataSubscriber.Add(modelDescriptionVariable.ValueReference, sub);
      }
      else if (modelDescriptionVariable.Causality == Variable.Causalities.Output)
      {
        var dataSpec = new PubSubSpec(modelDescriptionVariable.Name, Vector.MediaTypeData);
        var pub = silKitInstance.Participant.CreateDataPublisher(
          modelDescriptionVariable.Name,
          dataSpec, 0);
        silKitInstance.ValueRefToDataPublisher.Add(modelDescriptionVariable.ValueReference, pub);

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
    var variable = ModelDescription.Variables[valueRef];

    // buffer data
    // Currently, we use a last-is-best approach - this may be improved in the future

    if (dataMessageEvent.timestampInNs > nextSimStep)
    {
      throw new ArgumentOutOfRangeException(
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
      (name, status, category, message) =>
      {
        Console.WriteLine($"Logger: Name={name}; status={status}; category={category};\n  message={message}");
      }, status => throw new NotImplementedException("Step finished asynchronously"));

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
      (name, status, category, message) =>
      {
        Console.WriteLine($"Logger: Name={name}; status={status}; category={category};\n  message={message}");
      });


    fmi3Binding.EnterInitializationMode(
      ModelDescription.DefaultExperiment.Tolerance,
      ModelDescription.DefaultExperiment.StartTime,
      ModelDescription.DefaultExperiment.StopTime);

    // initialize all 'exact' and 'approx' values
    fmi3Binding.ExitInitializationMode();
  }

  private void InitializeSilKit(string silKitConfigurationPath, string participantName)
  {
    if (string.IsNullOrEmpty(participantName))
    {
      participantName = ModelDescription.ModelName;
    }

    Console.WriteLine($"-----------------------\n" +
                      $"Join SIL Kit simulation\n" +
                      $"Name: {participantName}\n" +
                      $"-----------------------\n");

    var wrapper = SilKitWrapper.Instance;
    ParticipantConfiguration config;
    if (String.IsNullOrEmpty(silKitConfigurationPath))
    {
      config = wrapper.GetConfigurationFromString("");
    }
    else
    {
      config = wrapper.GetConfigurationFromFile(silKitConfigurationPath);
    }

    silKitInstance = new SilKitInstance();

    var lc = new LifecycleService.LifecycleConfiguration(LifecycleService.LifecycleConfiguration.Modes.Coordinated);
    silKitInstance.Participant = wrapper.CreateParticipant(config, participantName);
    silKitInstance.LifecycleService = silKitInstance.Participant.CreateLifecycleService(lc);
    silKitInstance.TimeSyncService = silKitInstance.LifecycleService.CreateTimeSyncService();

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

    silKitInstance.TimeSyncService.SetSimulationStepHandler(SimulationStepReached, stepDuration);
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

    if (ModelDescription.DefaultExperiment.StopTime.HasValue)
    {
      if (fmiNow >= ModelDescription.DefaultExperiment.StopTime)
      {
        // stop the SIL Kit simulation
        silKitInstance.LifecycleService.Stop("FMU stopTime reached.");
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
        PublishData(result);
      }
      else if (varKvp.Key == typeof(double))
      {
        Binding.GetValue(valueRefArr, out ReturnVariable<double> result);
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
      var byteList = new List<byte>(variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<double> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<sbyte> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<byte> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<short> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
      for (int i = 0; i < variable.Values.Length; i++)
      {
        var byteArr = BitConverter.GetBytes(variable.Values[i]);
        FormatData(ref byteArr);
        byteList.AddRange(byteArr);
      }
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<ushort> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<int> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<uint> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<long> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<ulong> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<bool> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<string> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var byteList = new List<byte>(variable.Values.Length);
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(byteList.ToArray());
    }
  }

  private void PublishData(ReturnVariable<IntPtr> data)
  {
    foreach (var variable in data.ResultArray)
    {
      var publishableBytes = new List<byte>();

      var arraySize = variable.ValueSizes.Length;
      if (!variable.IsScalar)
      {
        var arrayPrefix = BitConverter.GetBytes(variable.Values.Length);
        FormatData(ref arrayPrefix);
        publishableBytes.AddRange(arrayPrefix);
      }
      
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
      silKitInstance.ValueRefToDataPublisher[variable.ValueReference].Publish(publishableBytes.ToArray());
    }
  }

  private void FormatData(ref byte[] bytes)
  {
    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
  }

  public void RunSimulation()
  {
    silKitInstance.LifecycleService.StartLifecycle();
    silKitInstance.LifecycleService.WaitForLifecycleToComplete();
  }
}


internal class Program
{
  static async Task Main(string[] args)
  {
    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand("FMU Importer for SIL Kit");

    var fmuPathOption = new Option<string>(
      name: "--fmu-path",
      description: "Set the path to the FMU file (.fmu). This is mandatory.");
    fmuPathOption.AddAlias("-f");
    fmuPathOption.IsRequired = true;
    rootCommand.AddOption(fmuPathOption);

    var silKitConfigFileOption = new Option<string>(
      name: "--sil-kit-config-file",
      description: "Set the path to the SIL Kit configuration file. Defaults to an empty configuration.");
    silKitConfigFileOption.AddAlias("-c");
    rootCommand.AddOption(silKitConfigFileOption);

    var participantNameOption = new Option<string>(
      name: "--participant-name",
      description: "Set the name of the SIL Kit participant. Defaults to the FMU's model name.");
    participantNameOption.AddAlias("-p");
    rootCommand.AddOption(participantNameOption);

    rootCommand.SetHandler((fmuPath, silKitConfigFile, participantName) =>
    {
      var instance = new FmuImporter(fmuPath, silKitConfigFile, participantName);
      instance.RunSimulation();
    }, fmuPathOption, silKitConfigFileOption, participantNameOption);

    await rootCommand.InvokeAsync(args);
  }
}