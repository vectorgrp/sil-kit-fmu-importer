
using System.Drawing;
using System.Runtime.InteropServices;
using Fmi.Binding;

namespace Prototype
{
  public class BindingTest
  {
    // indicator for stepComplete
    TaskCompletionSource<int> stepComplete;

    public enum FmuTests
    {
      BouncingBall
    };
    public BindingTest(string fmuPath, int fmiVersion, FmuTests fmuTest)
    {
      if (fmiVersion == 2)
      {
        using var binding = Fmi2BindingFactory.CreateFmi2Binding(fmuPath);
        stepComplete = new TaskCompletionSource<int>();
        switch (fmuTest)
        {
          case FmuTests.BouncingBall:
            RunTestBouncingBall(binding);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(fmuTest), fmuTest, null);
        }
      }
      else
      {
        throw new ArgumentOutOfRangeException("Invalid FMI version");
      }

    }
    #region FMI 2.x
    private void RunTestBouncingBall(IFmi2Binding binding)
    {
      var functions = new Fmi2BindingCallbackFunctions(
        (name, status, category, message) =>
        {
          Console.WriteLine($"Logger: Name={name}; status={status}; category={category};\n  message={message}");
        }, status => throw new NotImplementedException("Step finished asynchronously"));

      binding.Instantiate(
        "BouncingBall",
        "{1AE5E10D-9521-4DE3-80B9-D0EAAA7D5AF1}",
        functions,
        true,
        true);

      binding.SetDebugLogging(true, Array.Empty<string>());

      binding.SetupExperiment(
        null,
        0d,
        10d);

      // initialize all 'exact' and 'approx' values
      binding.SetReal(new uint[] { 5, 6 }, new[] { -9.81, 0.7 });

      binding.EnterInitializationMode();

      // in theory, do initialization here!

      binding.ExitInitializationMode();
      
      // run to the moon
      RunSimulation(binding);

      binding.Terminate();
    }

    private void RunSimulation(IFmi2Binding binding)
    {
      const double startTime = 0d;
      const double stopTime = 3d;
      const double stepSize = 1e-2;

      Console.WriteLine("[t]; [h]");

      double currentTime = startTime;
      while (currentTime < stopTime)
      {
        try
        {
          DoStep(currentTime, stepSize, binding);

          // retrieve variables
          var output = binding.GetReal(new uint[] { 0, 1 });

          currentTime = output[0];

          Console.WriteLine($"{String.Format("{0:0.00}", Math.Round(currentTime, 2))};\t{output[1]}");
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
          throw;
        }
      }
      
    }

    private void DoStep(double currentTime, double stepSize, IFmi2Binding binding)
    {
      stepComplete = new TaskCompletionSource<int>();
      binding.DoStep(currentTime, stepSize);
    }
    #endregion FMI 2.x
    
  }
}
