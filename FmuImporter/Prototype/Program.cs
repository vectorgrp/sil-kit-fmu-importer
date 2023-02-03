using System.Globalization;
using Fmi.Binding;
using Fmi.FmiModel;
using SilKit;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;
#pragma warning disable CS0162

namespace Prototype
{
  public class Demo
  {
    private const bool testSilKitBinding = false;
    private const bool testFmi2 = true;
    private const bool testFmi3 = true;

    public Demo()
    {
      if (testFmi2)
      {
        const string fmiFmuPathBouncingBall = @"FMUs\FMI2.0\BouncingBall.fmu";
        var binding1 = new BindingTest(fmiFmuPathBouncingBall, 2, BindingTest.FmuTests.BouncingBall);
      }

      if (testFmi3)
      {
        const string fmiFmuPathBouncingBall = @"FMUs\FMI3.0\BouncingBall.fmu";
        var extractedFmuPath = ModelLoader.ExtractFmu(fmiFmuPathBouncingBall);
        var modelDescription = ModelLoader.LoadModelFromExtractedPath(extractedFmuPath);
        Console.WriteLine("\n-----------------\n---- FMI 3.0 ----\n-----------------");
        Console.WriteLine(modelDescription.ToString());
        ModelLoader.RemoveExtractedFmu(fmiFmuPathBouncingBall);
      }

      if (testSilKitBinding)
      {
        var pInvokeTest = new PInvokeTest();
        pInvokeTest.RunSilKitPInvokeTest();
      }
    }

    // Entry point
    public static void Main(string[] args)
    {
      // Set output to be OS language independent
      CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
      CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
      new Demo();
    }
  }

  public class PInvokeTest
  {
    public PInvokeTest() { }

    public void RunSilKitPInvokeTest()
    {
      var wrapper = SilKitWrapper.Instance;
      // get config
      var config = wrapper.GetConfigurationFromFile("Config.silkit.yaml");
      // get participant
      var participant = wrapper.CreateParticipant(config, "Test", "silkit://127.0.0.1:8500");

      // configure PubSub
      var spec = new PubSubSpec("CommonTopic", "Blub");
      var publisher = participant.CreateDataPublisher("PublisherTest", spec, 1);
      var subscriber = participant.CreateDataSubscriber("SubscriberTest", spec, (DataMessageEvent e) =>
      {
        var ts = e.timestampInNs;
        var data = e.data;

        Console.WriteLine($"Received data @ {ts / 1e6}ms: '{string.Join("; ", data)}'");
      });

      // configure lifecycle service
      var lc = new LifecycleService.LifecycleConfiguration(LifecycleService.LifecycleConfiguration.Modes.Coordinated);
      var lifecycleService = participant.CreateLifecycleService(lc);
      lifecycleService.SetCommunicationReadyHandler(() =>
      {
        Console.WriteLine("CommunicationReady triggered!");
      });
      lifecycleService.SetStopHandler(() =>
      {
        Console.WriteLine("StopHandler triggered!");
      });
      lifecycleService.SetShutdownHandler(() =>
      {
        Console.WriteLine("ShutdownHandler triggered!");
      });

      // configure time sync service & sim step
      var timeSyncService = lifecycleService.CreateTimeSyncService();
      timeSyncService.SetSimulationStepHandler((ulong now, ulong duration) =>
      {
        byte baseCount = (byte)Math.Floor(now / 1e6);
        Console.WriteLine($"Now = {now / 1e6}; duration = {duration / 1e6}");
        publisher.Publish(new List<byte>() { (byte)(1 + baseCount), (byte)(2 + baseCount), (byte)(3 + baseCount), (byte)(4 + baseCount) });
        // stop simulation after 10ms
        if (now >= 10 * 1e6)
        {
          lifecycleService.Stop("Reached >= 10ms. Stop simulation!");
        }
      }, (ulong)(1e6));

      // start simulation
      lifecycleService.StartLifecycle();
      // wait for simulation completion
      lifecycleService.WaitForLifecycleToComplete();
    }
  }
}