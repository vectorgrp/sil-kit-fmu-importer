
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
      BouncingBall,
      Feedthrough
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
          case FmuTests.Feedthrough:
            throw new NotImplementedException();
            RunTestFeedthrough(binding);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(fmuTest), fmuTest, null);
        }
      }
      else if (fmiVersion == 3)
      {
        using var binding = Fmi3BindingFactory.CreateFmi3Binding(fmuPath);
        stepComplete = new TaskCompletionSource<int>();
        switch (fmuTest)
        {
          case FmuTests.BouncingBall:
            RunTestBouncingBall(binding);
            break;
          case FmuTests.Feedthrough:
            RunTestFeedthrough(binding);
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
      binding.DoStep(currentTime, stepSize, out _);
    }
    #endregion FMI 2.x

    #region FMI 3.0
    private void RunTestBouncingBall(IFmi3Binding binding)
    {
      binding.InstantiateCoSimulation(
        "BouncingBall",
        "{1AE5E10D-9521-4DE3-80B9-D0EAAA7D5AF1}",
        true,
        true,
        (name, status, category, message) =>
        {
          Console.WriteLine($"Logger: Name={name}; status={status}; category={category};\n  message={message}");
        });


      binding.EnterInitializationMode(
        null,
        0d,
        3d);

      // initialize all 'exact' and 'approx' values
      binding.SetFloat64(new uint[] {1, 5, 6 }, new[] { 2, -9.81, 0.7 });
      // in theory, do initialization here!

      binding.ExitInitializationMode();
      
      // run to the moon
      RunSimulation(binding);

      binding.Terminate();
    }

    private void RunSimulation(IFmi3Binding binding)
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
          var output = binding.GetFloat64(new uint[] { 0, 1 });

          currentTime = output.Values[0];

          Console.WriteLine($"{String.Format("{0:0.00}", Math.Round(currentTime, 2))};\t{output.Values[1]}");
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
          throw;
        }
      }
    }

    private void DoStep(double currentTime, double stepSize, IFmi3Binding binding)
    {
      stepComplete = new TaskCompletionSource<int>();
      binding.DoStep(currentTime, stepSize, out _);
    }

    private void RunTestFeedthrough(IFmi2Binding binding)
    {
    }

    private void RunTestFeedthrough(IFmi3Binding binding)
    {
      binding.InstantiateCoSimulation(
        "Feedthrough",
        "{37B954F1-CC86-4D8F-B97F-C7C36F6670D2}",
        true,
        true,
        (name, status, category, message) =>
        {
          Console.WriteLine($"Logger: Name={name}; status={status}; category={category};\n  message={message}");
          if (status == Fmi3Statuses.Error || status == Fmi3Statuses.Fatal)
          {
            throw new InvalidOperationException(message);
          }
        });

      float[] f32In = new float[2];
      //f32In = binding.GetFloat32(new[] { 1, 3 });
      double[] f64In = new double[3];
      //f64In = binding.GetFloat64(new[] { 5, 7, 9 });

      sbyte[] int8In = new sbyte[1];
      //int8In = binding.GetInt8(new[] { 11 });
      int8In = Enumerable.Repeat((sbyte)5, int8In.Length).ToArray();
      binding.SetInt8(new uint[] { 11 }, int8In);
      byte[] uint8In = new byte[1];
      //uint8In = binding.GetUInt8(new[] { 13 });
      short[] int16In = new short[1];
      //int16In = binding.GetInt16(new[] { 15 });
      ushort[] uint16In = new ushort[1];
      //uint16In = binding.GetUInt16(new[] { 17 });
      int[] int32In = new int[1];
      //int32In = binding.GetInt32(new[] { 19 });
      uint[] uint32In = new uint[1];
      //uint32In = binding.GetUInt32(new[] { 21 });
      long[] int64In = new long[1];
      //int64In = binding.GetInt64(new[] { 23 });
      ulong[] uint64In = new ulong[1];
      //uint64In = binding.GetUInt64(new[] { 25 });
      bool[] boolIn = new bool[1];
      //boolIn = binding.GetBoolean(new[] { 27 });
      //var stringParameterParam = binding.GetUInt64(new[] { 29 });
      IntPtr[] binaryIn = new IntPtr[1];
      //var binaryIn = binding.GetBinary(new[] { 30 });

      binding.EnterInitializationMode(
        null,
        0d,
        2d);

      binding.ExitInitializationMode();


      float[] f32Out;
      double[] f64Out;

      sbyte[] int8Out;
      byte[] uint8Out;
      short[] int16Out;
      ushort[] uint16Out;
      int[] int32Out;
      uint[] uint32Out;
      long[] int64Out;
      ulong[] uint64Out;

      bool[] boolOut;
      ReturnVariable<IntPtr> binaryOut;



      var currentTime = 0d;
      var internalStepSize = 0.1;

      for (int i = 1; i < 4; i++)
      {
        binding.DoStep(currentTime, internalStepSize, out currentTime);

        f32Out = binding.GetFloat32(new uint[] { 2, 4 }).Values;
        f64Out = binding.GetFloat64(new uint[] { 6, 8, 10 }).Values;

        int8Out = binding.GetInt8(new uint[] { 12 }).Values;
        uint8Out = binding.GetUInt8(new uint[] { 14 }).Values;
        int16Out = binding.GetInt16(new uint[] { 16 }).Values;
        uint16Out = binding.GetUInt16(new uint[] { 18 }).Values;
        int32Out = binding.GetInt32(new uint[] { 20 }).Values;
        uint32Out = binding.GetUInt32(new uint[] { 22 }).Values;
        int64Out = binding.GetInt64(new uint[] { 24 }).Values;
        uint64Out = binding.GetUInt64(new uint[] { 26 }).Values;

        boolOut = binding.GetBoolean(new uint[] { 28 }).Values;
        binaryOut = binding.GetBinary(new uint[] { 31 });

        var valueSize = binaryOut.NValueSizes?[0];
        if (valueSize == null)
          throw new ArgumentOutOfRangeException("ValueSize must be > 0");
        var outBytes = new byte[(int)valueSize];
        Marshal.Copy(binaryOut.Values[0], outBytes, 0, outBytes.Length);

        ;

        f32In = Enumerable.Repeat((float)i, f32In.Length).ToArray();
        binding.SetFloat32(new uint[] { 1, 3 }, f32In);
        f64In = Enumerable.Repeat((double)i, f64In.Length).ToArray();
        binding.SetFloat64(new uint[] { 5, 7, 9 }, f64In);

        int8In = Enumerable.Repeat((sbyte)i, int8In.Length).ToArray();
        binding.SetInt8(new uint[] { 11 }, int8In);
        uint8In = Enumerable.Repeat((byte)i, uint8In.Length).ToArray();
        binding.SetUInt8(new uint[] { 13 }, uint8In);
        int16In = Enumerable.Repeat((short)i, int16In.Length).ToArray();
        binding.SetInt16(new uint[] { 15 }, int16In);
        uint16In = Enumerable.Repeat((ushort)i, uint16In.Length).ToArray();
        binding.SetUInt16(new uint[] { 17 }, uint16In);
        int32In = Enumerable.Repeat((int)i, int32In.Length).ToArray();
        binding.SetInt32(new uint[] { 19 }, int32In);
        uint32In = Enumerable.Repeat((uint)i, uint32In.Length).ToArray();
        binding.SetUInt32(new uint[] { 21 }, uint32In);
        int64In = Enumerable.Repeat((long)i, int64In.Length).ToArray();
        binding.SetInt64(new uint[] { 23 }, int64In);
        uint64In = Enumerable.Repeat((ulong)i, uint64In.Length).ToArray();
        binding.SetUInt64(new uint[] { 25 }, uint64In);

        boolIn = Enumerable.Repeat(i % 2 == 0, boolIn.Length).ToArray();
        binding.SetBoolean(new uint[] { 27 }, boolIn);

        var inBytes = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2), (byte)(i + 3) };
        var inBytesHandler = GCHandle.Alloc(inBytes, GCHandleType.Pinned);
        var inBytesPtr = inBytesHandler.AddrOfPinnedObject();
        Marshal.Copy(inBytes, 0, inBytesPtr, 4);
        binaryIn = new[] { inBytesPtr };
        var inSize = new IntPtr[] { new IntPtr(4) };

        binding.SetBinary(new uint[] { 30 }, inSize, binaryIn);

      }
    }

    #endregion FMI 3.0

  }
}
