using System.Globalization;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using SilKit;
using SilKit.Services.Orchestration;

namespace FmuImporter;

public class FmuImporter
{
  private struct SilKitInstance
  {
    public Participant Participant { get; set; }
    public ILifecycleService LifecycleService { get; set; }
    public LifecycleService.ITimeSyncService TimeSyncService { get; set; }
  }


  ModelDescription ModelDescription { get; set; }
  private IFmiBindingCommon Binding { get; set; }

  private SilKitInstance silKitInstance;

  public FmuImporter(string fmuPath)
  {
    InitializeFMU(fmuPath);
    InitializeSilKit();
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

  private void InitializeSilKit()
  {
    Console.WriteLine($"-----------------------\n" +
                      $"Join SIL Kit simulation\n" +
                      $"Name: {ModelDescription.ModelName}\n" +
                      $"-----------------------\n");

    var wrapper = SilKitWrapper.Instance;
    var config = wrapper.GetConfigurationFromString("");
    var lc = new LifecycleService.LifecycleConfiguration(LifecycleService.LifecycleConfiguration.Modes.Coordinated);

    silKitInstance = new SilKitInstance();
    silKitInstance.Participant = wrapper.CreateParticipant(config, ModelDescription.ModelName);
    silKitInstance.LifecycleService = silKitInstance.Participant.CreateLifecycleService(lc);
    silKitInstance.TimeSyncService = silKitInstance.LifecycleService.CreateTimeSyncService();
    
    var stepDuration = (ModelDescription.DefaultExperiment.StepSize.HasValue)
      ? Helpers.FmiTimeToSilKitTime(ModelDescription.DefaultExperiment.StepSize.Value)
      : Helpers.DefaultSimStepDuration;
    silKitInstance.TimeSyncService.SetSimulationStepHandler(SimulationStepReached, stepDuration);
  }

  private void SimulationStepReached(ulong nowInNs, ulong durationInNs)
  {
    if (nowInNs == 0)
    {
      // skip initialization - it was done already.
      return;
    }

    var fmiNow = Helpers.SilKitTimeToFmiTime(nowInNs - durationInNs);
    Binding.DoStep(
      fmiNow,
      Helpers.SilKitTimeToFmiTime(durationInNs),
      out _);

    var output = ((IFmi3Binding)Binding).GetFloat64(new uint[] { 0, 1 });
    Console.WriteLine($"{nowInNs};{String.Format("{0:0.00}", Math.Round(output.Values[0], 2))};{output.Values[1]}");

    if (ModelDescription.DefaultExperiment.StopTime.HasValue)
    {
      if (fmiNow >= ModelDescription.DefaultExperiment.StopTime)
      {
        // stop the SIL Kit simulation
        silKitInstance.LifecycleService.Stop("FMU stopTime reached.");
        Binding.Terminate();
      }
    }
  }

  public void RunSimulation()
  {
    silKitInstance.LifecycleService.StartLifecycle();
    silKitInstance.LifecycleService.WaitForLifecycleToComplete();
  }
}


internal class Program
{
  private static string fmuPath = @"FMUs\FMI3.0\BouncingBall.fmu";

  static void Main(string[] args)
  {
    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    var instance = new FmuImporter(fmuPath);
    instance.RunSimulation();

  }
}