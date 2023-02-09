using System.Runtime.InteropServices;
using Fmi.FmiModel.Internal;

#pragma warning disable CS8618 // non-nullable field must be initialized in constructor

namespace Fmi.Binding;

public delegate void Logger(
  string instanceName,
  Fmi3Statuses status,
  string category,
  string message);

public enum Fmi3Statuses : int
{
  OK,
  Warning,
  Discard,
  Error,
  Fatal
}
public static class Fmi3BindingFactory
{
  public static IFmi3Binding CreateFmi3Binding(string fmuPath)
  {
    return new Fmi3Binding(fmuPath);
  }
}

public interface IFmi3Binding : IDisposable, IFmiBindingCommon
{
  public ModelDescription GetModelDescription();

  // Common & Co-Simulation Functions for FMI 3.0
  public void InstantiateCoSimulation(
    string instanceName,
    string instantiationToken,
    bool visible,
    bool loggingOn,
    Logger logger);

  public void EnterInitializationMode(double? tolerance, double startTime, double? stopTime);
  public void ExitInitializationMode();

  // Getters & Setters
  public ReturnVariable<float> GetFloat32(fmi3ValueReference[] valueReferences);
  public ReturnVariable<double> GetFloat64(fmi3ValueReference[] valueReferences);
  public ReturnVariable<sbyte> GetInt8(fmi3ValueReference[] valueReferences);
  public ReturnVariable<byte> GetUInt8(fmi3ValueReference[] valueReferences);
  public ReturnVariable<short> GetInt16(fmi3ValueReference[] valueReferences);
  public ReturnVariable<ushort> GetUInt16(fmi3ValueReference[] valueReferences);
  public ReturnVariable<int> GetInt32(fmi3ValueReference[] valueReferences);
  public ReturnVariable<uint> GetUInt32(fmi3ValueReference[] valueReferences);
  public ReturnVariable<long> GetInt64(fmi3ValueReference[] valueReferences);
  public ReturnVariable<ulong> GetUInt64(fmi3ValueReference[] valueReferences);
  public ReturnVariable<bool> GetBoolean(fmi3ValueReference[] valueReferences);
  public ReturnVariable<string> GetString(fmi3ValueReference[] valueReferences);
  public ReturnVariable<fmi3Binary> GetBinary(fmi3ValueReference[] valueReferences);
  public void SetFloat32(fmi3ValueReference[] valueReferences, float[] values);
  public void SetFloat64(fmi3ValueReference[] valueReferences, double[] values);
  public void SetInt8(fmi3ValueReference[] valueReferences, sbyte[] values);
  public void SetUInt8(fmi3ValueReference[] valueReferences, byte[] values);
  public void SetInt16(fmi3ValueReference[] valueReferences, short[] values);
  public void SetUInt16(fmi3ValueReference[] valueReferences, ushort[] values);
  public void SetInt32(fmi3ValueReference[] valueReferences, int[] values);
  public void SetUInt32(fmi3ValueReference[] valueReferences, uint[] values);
  public void SetInt64(fmi3ValueReference[] valueReferences, long[] values);
  public void SetUInt64(fmi3ValueReference[] valueReferences, ulong[] values);
  public void SetBoolean(fmi3ValueReference[] valueReferences, bool[] values);
  public void SetString(fmi3ValueReference[] valueReferences, string[] values);
  public void SetBinary(fmi3ValueReference[] valueReferences, IntPtr[] valueSizes, IntPtr[] values);
}

internal class Fmi3Binding : FmiBindingBase, IFmi3Binding
{
  private IntPtr component;

  public Fmi3Binding(string fmuPath) : base(fmuPath, "/binaries/x86_64-windows")
  {
  }

  protected override void InitializeFmiDelegates()
  {
    // Common functions
    SetDelegate(out fmi3InstantiateCoSimulation);
    SetDelegate(out fmi3FreeInstance);
    SetDelegate(out fmi3EnterInitializationMode);
    SetDelegate(out fmi3ExitInitializationMode);
    SetDelegate(out fmi3EnterStepMode);
    SetDelegate(out fmi3DoStep);
    SetDelegate(out fmi3Terminate);


    // Variable Getters and Setters
    SetDelegate(out fmi3GetFloat32);
    SetDelegate(out fmi3GetFloat64);
    SetDelegate(out fmi3GetInt8);
    SetDelegate(out fmi3GetUInt8);
    SetDelegate(out fmi3GetInt16);
    SetDelegate(out fmi3GetUInt16);
    SetDelegate(out fmi3GetInt32);
    SetDelegate(out fmi3GetUInt32);
    SetDelegate(out fmi3GetInt64);
    SetDelegate(out fmi3GetUInt64);
    SetDelegate(out fmi3GetBoolean);
    SetDelegate(out fmi3GetString);
    SetDelegate(out fmi3GetBinary);
    SetDelegate(out fmi3SetFloat32);
    SetDelegate(out fmi3SetFloat64);
    SetDelegate(out fmi3SetInt8);
    SetDelegate(out fmi3SetUInt8);
    SetDelegate(out fmi3SetInt16);
    SetDelegate(out fmi3SetUInt16);
    SetDelegate(out fmi3SetInt32);
    SetDelegate(out fmi3SetUInt32);
    SetDelegate(out fmi3SetInt64);
    SetDelegate(out fmi3SetUInt64);
    SetDelegate(out fmi3SetBoolean);
    SetDelegate(out fmi3SetString);
    SetDelegate(out fmi3SetBinary);
  }
  public ModelDescription GetModelDescription()
  {
    return ModelDescription;
  }

  #region Common & Co-Simulation Functions for FMI 3.0

  public void InstantiateCoSimulation(
    string instanceName,
    string instantiationToken,
    bool visible,
    bool loggingOn,
    Logger logger)
  {
    component = fmi3InstantiateCoSimulation(
      instanceName,
      instantiationToken,
      $"file://{FullFmuLibraryPath}/resources",
      visible,
      loggingOn,
      false, 
      false, 
      IntPtr.Zero,
      IntPtr.Zero, 
      IntPtr.Zero,
      logger,
      IntPtr.Zero);
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("Failed to create an FMU instance");
    }
  }
  /*
    typedef fmi3Instance fmi3InstantiateCoSimulationTYPE(
      fmi3String                     instanceName,
      fmi3String                     instantiationToken,
      fmi3String                     resourcePath,
      fmi3Boolean                    visible,
      fmi3Boolean                    loggingOn,
      fmi3Boolean                    eventModeUsed,
      fmi3Boolean                    earlyReturnAllowed,
      const fmi3ValueReference       requiredIntermediateVariables[],
      size_t                         nRequiredIntermediateVariables,
      fmi3InstanceEnvironment        instanceEnvironment,
      fmi3LogMessageCallback         logMessage,
      fmi3IntermediateUpdateCallback intermediateUpdate);
  */
  internal fmi3InstantiateCoSimulationTYPE fmi3InstantiateCoSimulation;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate IntPtr fmi3InstantiateCoSimulationTYPE(
    string instanceName,
    string instantiationToken,
    string resourcePath,
    bool visible,
    bool loggingOn,
    bool eventModeUsed,
    bool earlyReturnAllowed,
    IntPtr requiredIntermediateVariables,
    size_t nRequiredIntermediateVariables,
    IntPtr instanceEnvironment,
    Logger logMessage,
    IntPtr intermediateUpdate);

  // TODO free in dispose
  /*
    typedef void fmi3FreeInstanceTYPE(fmi3Instance instance);
  */
  internal fmi3FreeInstanceTYPE fmi3FreeInstance;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3FreeInstanceTYPE(IntPtr instance);

  public void EnterInitializationMode(double? tolerance, double startTime, double? stopTime)
  {
    var fmi3Status = fmi3EnterInitializationMode(
      component,
      tolerance.HasValue,
      (tolerance.HasValue) ? tolerance.Value : double.NaN,
      startTime,
      stopTime.HasValue,
      (stopTime.HasValue) ? stopTime.Value : double.NaN);
    if (fmi3Status != 0)
    {
      throw new InvalidOperationException($"fmi3EnterInitializationMode exited with exit code '{fmi3Status}'.");
    }
  }
  /*
    typedef fmi3Status fmi3EnterInitializationModeTYPE(
      fmi3Instance instance,
      fmi3Boolean  toleranceDefined,
      fmi3Float64  tolerance,
      fmi3Float64  startTime,
      fmi3Boolean  stopTimeDefined,
      fmi3Float64  stopTime);
   */
  internal fmi3EnterInitializationModeTYPE fmi3EnterInitializationMode;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3EnterInitializationModeTYPE(
    IntPtr instance,
    bool toleranceDefined,
    double tolerance,
    double startTime,
    bool stopTimeDefined,
    double stopTime);

  public void ExitInitializationMode()
  {
    var fmi3Status = fmi3ExitInitializationMode(component);
    if (fmi3Status != 0)
    {
      throw new InvalidOperationException(
        $"Internal call to fmi3ExitInitializationMode exited with exit code '{fmi3Status}'.");
    }
  }
  /*
    typedef fmi3Status fmi3ExitInitializationModeTYPE(fmi3Instance instance);
   */
  internal fmi3ExitInitializationModeTYPE fmi3ExitInitializationMode;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3ExitInitializationModeTYPE(IntPtr instance);

  public void EnterStepMode()
  {
    var fmi3Status = fmi3EnterStepMode(component);
    if (fmi3Status != 0)
    {
      throw new InvalidOperationException(
        $"Internal call to fmi3EnterStepMode exited with exit code '{fmi3Status}'.");
    }
  }
  /*
    typedef fmi3Status fmi3EnterStepModeTYPE(fmi3Instance instance);
  */
  internal fmi3EnterStepModeTYPE fmi3EnterStepMode;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3EnterStepModeTYPE(IntPtr instance);

  public override void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime)
  {
    bool terminateRequested;
    var fmi3Status = fmi3DoStep(component, currentCommunicationPoint, communicationStepSize, true, out _, out terminateRequested, out _, out lastSuccessfulTime);
    if (fmi3Status != 0)
    {
      throw new InvalidOperationException(
        $"Internal call to fmi3EnterStepMode exited with exit code '{fmi3Status}'.");
    }

    if (terminateRequested)
    {
      Console.WriteLine("FMU requested simulation termination. Calling fmi3Terminate.");
      fmi3Terminate(component);
    }
  }
  /*
    typedef fmi3Status fmi3DoStepTYPE(
      fmi3Instance instance,
      fmi3Float64 currentCommunicationPoint,
      fmi3Float64 communicationStepSize,
      fmi3Boolean noSetFMUStatePriorToCurrentPoint,
      fmi3Boolean* eventHandlingNeeded,
      fmi3Boolean* terminateSimulation,
      fmi3Boolean* earlyReturn,
      fmi3Float64* lastSuccessfulTime);
  */
  internal fmi3DoStepTYPE fmi3DoStep;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3DoStepTYPE(
    IntPtr instance,
    double currentCommunicationPoint,
    double communicationStepSize,
    bool noSetFMUStatePriorToCurrentPoint,
    out bool eventHandlingNeeded,
    out bool terminateSimulation,
    out bool earlyReturn,
    out double lastSuccessfulTime);

  public override void Terminate()
  {
    var fmi3Status = fmi3Terminate(component);
    if (fmi3Status != 0)
    {
      throw new InvalidOperationException(
        $"Internal call to fmi3Terminate exited with exit code '{fmi3Status}'.");
    }
  }
  /*
    typedef fmi3Status fmi3TerminateTYPE(fmi3Instance instance);
   */
  internal fmi3TerminateTYPE fmi3Terminate;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3TerminateTYPE(IntPtr instance);

  #endregion Common & Co-Simulation Functions for FMI 3.0

  #region Getters & Setters
  /////////////
  // Getters //
  /////////////
  public ReturnVariable<float> GetFloat32(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new float[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetFloat32(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<float>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetFloat32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Float32 values[],
      size_t nValues);
  */
  internal fmi3GetFloat32TYPE fmi3GetFloat32;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetFloat32TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    float[] values,
    size_t nValues);
  
  public ReturnVariable<double> GetFloat64(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new double[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetFloat64(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<double>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetFloat64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Float64 values[],
      size_t nValues);
  */
  internal fmi3GetFloat64TYPE fmi3GetFloat64;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetFloat64TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    double[] values,
    size_t nValues);

  public ReturnVariable<sbyte> GetInt8(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new sbyte[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetInt8(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<sbyte>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetInt8TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int8 values[],
      size_t nValues);
  */
  internal fmi3GetInt8TYPE fmi3GetInt8;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetInt8TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    sbyte[] values,
    size_t nValues);

  public ReturnVariable<byte> GetUInt8(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new byte[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetUInt8(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<byte>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetUInt8TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt8 values[],
      size_t nValues);
  */
  internal fmi3GetUInt8TYPE fmi3GetUInt8;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetUInt8TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    byte[] values,
    size_t nValues);

  public ReturnVariable<short> GetInt16(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new short[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetInt16(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<short>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetInt16TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int16 values[],
      size_t nValues);
  */
  internal fmi3GetInt16TYPE fmi3GetInt16;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetInt16TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    short[] values,
    size_t nValues);

  public ReturnVariable<ushort> GetUInt16(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new ushort[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetUInt16(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<ushort>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetUInt16TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt16 values[],
      size_t nValues);
  */
  internal fmi3GetUInt16TYPE fmi3GetUInt16;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetUInt16TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    ushort[] values,
    size_t nValues);

  public ReturnVariable<int> GetInt32(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new int[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetInt32(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<int>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetInt32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int32 values[],
      size_t nValues);
  */
  internal fmi3GetInt32TYPE fmi3GetInt32;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetInt32TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    int[] values,
    size_t nValues);

  public ReturnVariable<uint> GetUInt32(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new uint[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetUInt32(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<uint>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetUInt32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt32 values[],
      size_t nValues);
  */
  internal fmi3GetUInt32TYPE fmi3GetUInt32;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetUInt32TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    uint[] values,
    size_t nValues);

  public ReturnVariable<long> GetInt64(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new long[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetInt64(component, valueReferences, (IntPtr)valueReferences.Length, result, nValues);

    return new ReturnVariable<long>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetInt64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int64 values[],
      size_t nValues);
  */
  internal fmi3GetInt64TYPE fmi3GetInt64;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetInt64TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    long[] values,
    size_t nValues);

  public ReturnVariable<ulong> GetUInt64(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new ulong[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetUInt64(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<ulong>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetUInt64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt64 values[],
      size_t nValues);
  */
  internal fmi3GetUInt64TYPE fmi3GetUInt64;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetUInt64TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    ulong[] values,
    size_t nValues);

  public ReturnVariable<bool> GetBoolean(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new bool[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetBoolean(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<bool>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetBooleanTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Boolean values[],
      size_t nValues);
  */
  internal fmi3GetBooleanTYPE fmi3GetBoolean;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetBooleanTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    bool[] values,
    size_t nValues);

  public ReturnVariable<string> GetString(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new string[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);

    fmi3GetString(component, valueReferences, (size_t)valueReferences.Length, result, nValues);

    return new ReturnVariable<string>(result, nValues);
  }
  /*
    typedef fmi3Status fmi3GetStringTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3String values[],
      size_t nValues);
  */
  internal fmi3GetStringTYPE fmi3GetString;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetStringTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    string[] values,
    size_t nValues);

  public ReturnVariable<fmi3Binary> GetBinary(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    var result = new fmi3Binary[valueReferences.Length];
    size_t[] valueSizes = new size_t[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);
    fmi3GetBinary(component, valueReferences, (size_t)valueReferences.Length, valueSizes, result, (size_t)nValues);
    
    return new ReturnVariable<fmi3Binary>(result, nValues, valueSizes);
  }
  /*
    typedef fmi3Status fmi3GetBinaryTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      size_t valueSizes[],
      fmi3Binary values[],
      size_t nValues);
  */
  internal fmi3GetBinaryTYPE fmi3GetBinary;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetBinaryTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    size_t[] valueSizes,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
    IntPtr[] values, // TODO convert to blittable ByteArray if possible
    size_t nValues);

  /////////////
  // Setters //
  /////////////
  public void SetFloat32(fmi3ValueReference[] valueReferences, float[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetFloat32(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetFloat32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Float32 values[],
      size_t nValues);
  */
  internal fmi3SetFloat32TYPE fmi3SetFloat32;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetFloat32TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    float[] values,
    size_t nValues);

  public void SetFloat64(fmi3ValueReference[] valueReferences, double[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetFloat64(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetFloat64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Float64 values[],
      size_t nValues);
  */
  internal fmi3SetFloat64TYPE fmi3SetFloat64;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetFloat64TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    double[] values,
    size_t nValues);

  public void SetInt8(fmi3ValueReference[] valueReferences, sbyte[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetInt8(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetInt8TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Int8 values[],
      size_t nValues);
  */
  internal fmi3SetInt8TYPE fmi3SetInt8;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetInt8TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    sbyte[] values,
    size_t nValues);

  public void SetUInt8(fmi3ValueReference[] valueReferences, byte[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetUInt8(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetUInt8TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3UInt8 values[],
      size_t nValues);
  */
  internal fmi3SetUInt8TYPE fmi3SetUInt8;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetUInt8TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    byte[] values,
    size_t nValues);

  public void SetInt16(fmi3ValueReference[] valueReferences, short[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetInt16(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetInt16TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Int16 values[],
      size_t nValues);
  */
  internal fmi3SetInt16TYPE fmi3SetInt16;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetInt16TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    short[] values,
    size_t nValues);

  public void SetUInt16(fmi3ValueReference[] valueReferences, ushort[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetUInt16(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetUInt16TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3UInt16 values[],
      size_t nValues);
  */
  internal fmi3SetUInt16TYPE fmi3SetUInt16;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetUInt16TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    ushort[] values,
    size_t nValues);

  public void SetInt32(fmi3ValueReference[] valueReferences, int[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetInt32(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetInt32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Int32 values[],
      size_t nValues);
  */
  internal fmi3SetInt32TYPE fmi3SetInt32;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetInt32TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    int[] values,
    size_t nValues);

  public void SetUInt32(fmi3ValueReference[] valueReferences, uint[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetUInt32(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetUInt32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3UInt32 values[],
      size_t nValues);
  */
  internal fmi3SetUInt32TYPE fmi3SetUInt32;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetUInt32TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    uint[] values,
    size_t nValues);

  public void SetInt64(fmi3ValueReference[] valueReferences, long[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetInt64(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetInt64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Int64 values[],
      size_t nValues);
  */
  internal fmi3SetInt64TYPE fmi3SetInt64;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetInt64TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    long[] values,
    size_t nValues);

  public void SetUInt64(fmi3ValueReference[] valueReferences, ulong[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetUInt64(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetUInt64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3UInt64 values[],
      size_t nValues);
  */
  internal fmi3SetUInt64TYPE fmi3SetUInt64;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetUInt64TYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    ulong[] values,
    size_t nValues);

  public void SetBoolean(fmi3ValueReference[] valueReferences, bool[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetBoolean(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetBooleanTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3Boolean values[],
      size_t nValues);
  */
  internal fmi3SetBooleanTYPE fmi3SetBoolean;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetBooleanTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    bool[] values,
    size_t nValues);

  public void SetString(fmi3ValueReference[] valueReferences, string[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetString(component, valueReferences, (size_t)valueReferences.Length, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetStringTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const fmi3String values[],
      size_t nValues);
  */
  internal fmi3SetStringTYPE fmi3SetString;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetStringTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    string[] values,
    size_t nValues);

  public void SetBinary(fmi3ValueReference[] valueReferences, IntPtr[] valueSizes, IntPtr[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }
    
    fmi3SetBinary(component, valueReferences, (size_t)valueReferences.Length, valueSizes, values, (size_t)values.Length);
  }
  /*
    typedef fmi3Status fmi3SetBinaryTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      const size_t valueSizes[],
      const fmi3Binary values[],
      size_t nValues);
  */
  internal fmi3SetBinaryTYPE fmi3SetBinary;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetBinaryTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
    IntPtr[] /* size_t */ valueSizes,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
    IntPtr[] values, // FIXME 
    size_t nValues);

  #endregion Getters & Setters

  private size_t CalculateValueLength(ref fmi3ValueReference[] valueReferences)
  {
    var valueSize = 0UL;
    foreach (var valueReference in valueReferences)
    {
      valueSize += ModelDescription.Variables[valueReference].FlattenedArrayLength;
    }

    return (size_t)valueSize;
  }

}