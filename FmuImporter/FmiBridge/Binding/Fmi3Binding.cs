using System.Runtime.InteropServices;
using Fmi.FmiModel.Internal;

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
#if OS_WINDOWS
  private const string osPath = "/binaries/x86_64-windows";
#elif OS_LINUX
  private const string osPath = "/binaries/x86_64-linux";
#elif OS_MAC
  private const string osPath = "/binaries/x86_64-darwin";
#endif

  public Fmi3Binding(string fmuPath) : base(fmuPath, osPath)
  {
    // Common functions
    SetDelegate(out fmi3InstantiateCoSimulation);
    SetDelegate(out fmi3FreeInstance);
    SetDelegate(out fmi3EnterInitializationMode);
    SetDelegate(out fmi3ExitInitializationMode);
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
      $"file://{FullFmuLibPath}/resources",
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
    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3EnterInitializationMode(
        component,
        tolerance.HasValue,
        (tolerance.HasValue) ? tolerance.Value : double.NaN,
        startTime,
        stopTime.HasValue,
        (stopTime.HasValue) ? stopTime.Value : double.NaN),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

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
    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3ExitInitializationMode(component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }
  /*
    typedef fmi3Status fmi3ExitInitializationModeTYPE(fmi3Instance instance);
   */
  internal fmi3ExitInitializationModeTYPE fmi3ExitInitializationMode;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3ExitInitializationModeTYPE(IntPtr instance);

  public override void GetValue(uint[] valueRefs, out ReturnVariable<float> result)
  {
    result = GetFloat32(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<double> result)
  {
    result = GetFloat64(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<sbyte> result)
  {
    result = GetInt8(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<byte> result)
  {
    result = GetUInt8(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<short> result)
  {
    result = GetInt16(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<ushort> result)
  {
    result = GetUInt16(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<int> result)
  {
    result = GetInt32(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<uint> result)
  {
    result = GetUInt32(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<long> result)
  {
    result = GetInt64(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<ulong> result)
  {
    result = GetUInt64(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<bool> result)
  {
    result = GetBoolean(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<string> result)
  {
    result = GetString(valueRefs);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<IntPtr> result)
  {
    result = GetBinary(valueRefs);
  }

  public override void SetValue(uint valueRef, byte[] data)
  {
    var mdVar = ModelDescription.Variables[valueRef];
    var type = mdVar.VariableType;

    var isScalar = !(mdVar.Dimensions != null && mdVar.Dimensions.Length > 0);

    int arraySize = 1;
    if (!isScalar)
    {
      arraySize = BitConverter.ToInt32(data, 0);
      data = data.Skip(4).ToArray();
    }

    if (type == typeof(float))
    {
      var sizeMatches = (data.Length / sizeof(float)) == arraySize;

      var values = new float[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToSingle(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetFloat32(new[] { valueRef }, values);
    }
    else if (type == typeof(double))
    {
      var values = new double[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToDouble(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetFloat64(new[] { valueRef }, values);
    }
    else if (type == typeof(sbyte))
    {
      if (isScalar)
      {
        if (data.Length > 1)
        {
          throw new NotSupportedException("Unexpected size of data type");
        }
      }

      var values = new sbyte[arraySize];
      Buffer.BlockCopy(data, 0, values, 0, data.Length);

      SetInt8(new[] { valueRef }, new[] { (sbyte)data[0] });
    }
    else if (type == typeof(byte))
    {
      if (isScalar)
      {
        if (data.Length > 1)
        {
          throw new NotSupportedException("Unexpected size of data type");
        }
      }

      var values = new sbyte[arraySize];

      SetUInt8(new[] { valueRef }, data);
    }
    else if (type == typeof(short))
    {
      var values = new short[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToInt16(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetInt16(new[] { valueRef }, values);
    }
    else if (type == typeof(ushort))
    {
      var values = new ushort[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToUInt16(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetUInt16(new[] { valueRef }, values);
    }
    else if (type == typeof(int))
    {
      var values = new int[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToInt32(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetInt32(new[] { valueRef }, values);
    }
    else if (type == typeof(uint))
    {
      var values = new uint[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToUInt32(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetUInt32(new[] { valueRef }, values);
    }
    else if (type == typeof(long))
    {
      var values = new long[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToInt64(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetInt64(new[] { valueRef }, values);
    }
    else if (type == typeof(ulong))
    {
      var values = new ulong[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToUInt64(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetUInt64(new[] { valueRef }, values);
    }
    else if (type == typeof(bool))
    {
      var values = new bool[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToBoolean(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetBoolean(new[] { valueRef }, values);
    }
    else if (type == typeof(string))
    {
      var values = new string[arraySize];

      if (isScalar)
      {
        var value = BitConverter.ToString(data);
        values = new[] { value };
      }
      else
      {
        Buffer.BlockCopy(data, 0, values, 0, data.Length);
      }

      SetString(new[] { valueRef }, values);
    }
    else if (type == typeof(IntPtr))
    {
      throw new NotSupportedException("Must be called with binSizes argument!");
    }
  }

  public override void SetValue(uint valueRef, byte[] data, int[] binSizes)
  {
    var mdVar = ModelDescription.Variables[valueRef];
    var type = mdVar.VariableType;

    var isScalar = !(mdVar.Dimensions != null && mdVar.Dimensions.Length > 0);

    int arraySize = 1;
    if (!isScalar)
    {
      arraySize = BitConverter.ToInt32(data, 0);
      data = data.Skip(4).ToArray();

      if (binSizes.Sum() != data.Length)
      {
        throw new ArgumentOutOfRangeException($"The expected data length ({binSizes.Sum()}) " +
                                              $"does not match the received data length ({data.Length}).");
      }
    }

    if (type == typeof(IntPtr))
    {
      var values = new IntPtr[arraySize];
      var handlers = new GCHandle[arraySize];

      if (isScalar)
      {
        var handler = GCHandle.Alloc(data, GCHandleType.Pinned);
        handlers[0] = handler;
        values = new[] { handler.AddrOfPinnedObject() };
      }
      else
      {
        int offset = 0;
        for (int i = 0; i < arraySize; i++)
        {
          var currentBinary = new byte[binSizes[i]]; 
          Buffer.BlockCopy(data, offset, currentBinary, 0, binSizes[i]);
          offset += binSizes[i];

          var handler = GCHandle.Alloc(currentBinary, GCHandleType.Pinned);
          handlers[i] = handler;
          values[i] = handler.AddrOfPinnedObject();
        }
      }
      SetBinary(new[] { valueRef }, new[] { (IntPtr)data.Length }, values);

      // TODO/FIXME this is leaking memory, as the objects will never be freed
    }
  }

  public override void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime)
  {
    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3DoStep(
        component, 
        currentCommunicationPoint, 
        communicationStepSize, 
        true, 
        out _, 
        out var terminateRequested, 
        out _, 
        out lastSuccessfulTime),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    
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
    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3Terminate(component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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
    
    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new float[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetFloat32(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);


    return ReturnVariable<float>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new double[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetFloat64(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<double>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new sbyte[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt8(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<sbyte>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new byte[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt8(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<byte>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new short[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt16(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<short>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new ushort[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt16(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<ushort>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new int[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt32(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<int>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new uint[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt32(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<uint>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new long[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt64(
        component, 
        valueReferences, 
        (IntPtr)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<long>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new ulong[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt64(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<ulong>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new bool[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetBoolean(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<bool>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new string[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetString(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<string>.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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
    
    size_t[] valueSizes = new size_t[valueReferences.Length];
    size_t nValues = CalculateValueLength(ref valueReferences);
    var result = new fmi3Binary[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetBinary(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        valueSizes, 
        result, 
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable<fmi3Binary>.CreateReturnVariable(
      valueReferences, 
      result, 
      nValues, 
      ref modelDescription, 
      valueSizes);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetFloat32(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetFloat64(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetInt8(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetUInt8(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetInt16(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetUInt16(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetInt32(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetUInt32(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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
    
    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetInt64(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetUInt64(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetBoolean(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetString(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetBinary(
        component, 
        valueReferences, 
        (size_t)valueReferences.Length, 
        valueSizes, 
        values, 
        (size_t)values.Length),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
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
    IntPtr[] values,
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