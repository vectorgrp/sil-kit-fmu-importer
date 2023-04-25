using System.Runtime.InteropServices;
using System.Text;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public struct Fmi2BindingCallbackFunctions
{
  public Fmi2BindingCallbackFunctions(Logger loggerDelegate, StepFinished stepFinishedDelegate)
  {
    internalCallbackFunctions = new fmi2CallbackFunctions()
    {
      logger = (environment, name, status, category, message) =>
      {
        loggerDelegate(name, (Fmi2Statuses)status, category, message);
      },
      // usage is discouraged per standard
      allocateMemory = (nobj, size) =>
      {
        return Marshal.AllocHGlobal((int)nobj * (int)size);
      },
      // usage is discouraged per standard
      freeMemory = Marshal.FreeHGlobal,
      stepFinished = (environment, status) =>
      {
        Fmi.Helpers.Log(Fmi.Helpers.LogSeverity.Debug, "Step finished with status = " + status);
        stepFinishedDelegate((Fmi2Statuses)status);
      },
      componentEnvironment = IntPtr.Zero,
    };
  }

  public delegate void Logger(
    string instanceName,
    Fmi2Statuses status,
    string category,
    string message);

  public delegate void StepFinished(Fmi2Statuses status);

  internal fmi2CallbackFunctions internalCallbackFunctions;

  /*
    typedef struct {
      fmi2CallbackLogger logger;
      fmi2CallbackAllocateMemory allocateMemory;
      fmi2CallbackFreeMemory freeMemory;
      fmi2StepFinished stepFinished;
      fmi2ComponentEnvironment componentEnvironment;
    }
    fmi2CallbackFunctions;
  */
  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate void fmi2CallbackLogger(
    IntPtr componentEnvironment,
    string instanceName,
    int status,
    string category,
    string message);

  internal delegate IntPtr fmi2CallbackAllocateMemory(IntPtr /*size_t*/ nobj, IntPtr /*size_t*/ size);

  internal delegate void fmi2CallbackFreeMemory(IntPtr nobj);

  internal delegate void fmi2StepFinished(IntPtr componentEnvironment, int status);

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal struct fmi2CallbackFunctions
  {
    internal fmi2CallbackLogger logger;
    internal fmi2CallbackAllocateMemory allocateMemory;
    internal fmi2CallbackFreeMemory freeMemory;
    internal fmi2StepFinished stepFinished;
    internal IntPtr componentEnvironment;
  }
}

public enum Fmi2Statuses : int
{
  OK,
  Warning,
  Discard,
  Error,
  Fatal,
  Pending
}

public interface IFmi2Binding : IFmiBindingCommon
{
  public ModelDescription GetModelDescription();

  // Common functions
  public void SetDebugLogging(bool loggingOn, string[] categories);

  public void Instantiate(
    string instanceName,
    string fmuGUID,
    Fmi2BindingCallbackFunctions functions,
    bool visible,
    bool loggingOn);

  public void SetupExperiment(double? tolerance, double startTime, double? stopTime);
  public void EnterInitializationMode();
  public void ExitInitializationMode();

  // Functions for FMI2 for Co-Simulation
  public void CancelStep();

  // Getters & Setters
  public double[] GetReal(uint[] valueReferences);
  public int[] GetInteger(uint[] valueReferences);
  public bool[] GetBoolean(uint[] valueReferences);
  public string[] GetString(uint[] valueReferences);
  public void SetReal(uint[] valueReferences, double[] values);
  public void SetInteger(uint[] valueReferences, int[] values);
  public void SetBoolean(uint[] valueReferences, bool[] values);
  public void SetString(uint[] valueReferences, string[] values);

  // Internal
  public void NotifyAsyncDoStepReturned(Fmi2Statuses status);
}

public static class Fmi2BindingFactory
{
  public static IFmi2Binding CreateFmi2Binding(string fmuPath)
  {
    return new Fmi2Binding(fmuPath);
  }
}

internal class Fmi2Binding : FmiBindingBase, IFmi2Binding
{
  private IntPtr component = IntPtr.Zero;

#if OS_WINDOWS
  private const string osPath = "/binaries/win64";
#elif OS_LINUX
  private const string osPath = "/binaries/linux64";
#elif OS_MAC
  private const string osPath = "/binaries/darwin64";
#endif

  private static AutoResetEvent waitForDoStepEvent = new AutoResetEvent(false);

  public Fmi2Binding(string fmuPath) : base(fmuPath, osPath)
  {
    // Common Functions
    SetDelegate(out fmi2SetDebugLogging);
    SetDelegate(out fmi2Instantiate);
    SetDelegate(out fmi2FreeInstance);
    SetDelegate(out fmi2SetupExperiment);
    SetDelegate(out fmi2EnterInitializationMode);
    SetDelegate(out fmi2ExitInitializationMode);
    SetDelegate(out fmi2Terminate);

    // Functions for FMI2 for Co-Simulation
    SetDelegate(out fmi2DoStep);
    SetDelegate(out fmi2CancelStep);

    // Variable Getters and Setters
    SetDelegate(out fmi2GetReal);
    SetDelegate(out fmi2GetInteger);
    SetDelegate(out fmi2GetString);
    SetDelegate(out fmi2GetBoolean);
    SetDelegate(out fmi2SetReal);
    SetDelegate(out fmi2SetInteger);
    SetDelegate(out fmi2SetString);
    SetDelegate(out fmi2SetBoolean);

    // other
    SetDelegate(out fmi2GetFMUstate);
  }

  #region IDisposable

  ~Fmi2Binding()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    fmi2FreeInstance(component);
  }

  private bool mDisposedValue;

  protected override void Dispose(bool disposing)
  {
    if (!mDisposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
      }

      ReleaseUnmanagedResources();
      mDisposedValue = true;
    }

    base.Dispose(disposing);
  }

  #endregion IDisposable

  public override void GetValue(uint[] valueRefs, out ReturnVariable<float> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<double> result)
  {
    var vFmi = GetReal(valueRefs);
    result = ReturnVariable<double>.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref modelDescription);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<sbyte> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<byte> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<short> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<ushort> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<int> result)
  {
    var vFmi = GetInteger(valueRefs);
    result = ReturnVariable<int>.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref modelDescription);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<uint> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<long> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<ulong> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<bool> result)
  {
    var vFmi = GetBoolean(valueRefs);
    result = ReturnVariable<bool>.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref modelDescription);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<string> result)
  {
    var vFmi = GetString(valueRefs);
    result = ReturnVariable<string>.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref modelDescription);
  }

  public override void GetValue(uint[] valueRefs, out ReturnVariable<IntPtr> result)
  {
    throw new NotSupportedException("Not available in FMI 2.0.");
  }

  public override void SetValue(uint valueRef, byte[] data)
  {
    var mdVar = ModelDescription.Variables[valueRef];
    SetValue(mdVar, data);
  }

  internal override void SetValue(Variable mdVar, byte[] data)
  {
    var type = mdVar.VariableType;

    if (mdVar.Dimensions != null)
    {
      throw new NotSupportedException("FMI 2 does not support arrays natively.");
    }

    if (type == typeof(double))
    {
      var value = BitConverter.ToDouble(data);

      // Apply unit transformation
      // FMU value = [SIL Kit value] * factor + offset
      var unit = mdVar.TypeDefinition?.Unit;
      if (unit != null)
      {
        // first apply factor, then offset
        if (unit.Factor.HasValue)
        {
          value *= unit.Factor.Value;
        }

        if (unit.Offset.HasValue)
        {
          value += unit.Offset.Value;
        }
      }

      SetReal(new[] { mdVar.ValueReference }, new[] { value });
    }
    else if (type == typeof(int))
    {
      var value = BitConverter.ToInt32(data);
      SetInteger(new[] { mdVar.ValueReference }, new[] { value });
    }
    else if (type == typeof(bool))
    {
      var value = BitConverter.ToBoolean(data);
      SetBoolean(new[] { mdVar.ValueReference }, new[] { value });
    }
    else if (type == typeof(string))
    {
      var byteLength = BitConverter.ToInt32(data, 0);
      // offset = 4 byte -> 32 bit
      var value = Encoding.UTF8.GetString(data, 4, byteLength);
      SetString(new[] { mdVar.ValueReference }, new[] { value });
    }
  }

  public override void SetValue(uint valueRef, byte[] data, int[] binSizes)
  {
    throw new NotSupportedException("The binary datatype is not available in FMI 2.0.");
  }

  public ModelDescription GetModelDescription()
  {
    return ModelDescription;
  }

  public void NotifyAsyncDoStepReturned(Fmi2Statuses status)
  {
    Helpers.ProcessReturnCode(status, System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    waitForDoStepEvent.Set();
  }

  #region Common & Co-Simulation Functions for FMI 2.x

  public void SetDebugLogging(bool loggingOn, string[] categories)
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2SetDebugLogging(component, loggingOn, (IntPtr)categories.Length, categories),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
  typedef fmi2Status  fmi2SetDebugLoggingTYPE(
    fmi2Component c,
    fmi2Boolean loggingOn,
    size_t nCategories,
    const fmi2String categories[]);
  */
  internal fmi2SetDebugLoggingTYPE fmi2SetDebugLogging;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2SetDebugLoggingTYPE(
    IntPtr c,
    bool loggingOn,
    IntPtr nCategories,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    string[] categories
  );

  public void Instantiate(
    string instanceName,
    string fmuGUID,
    Fmi2BindingCallbackFunctions functions,
    bool visible,
    bool loggingOn)
  {
    component = fmi2Instantiate(
      instanceName,
      1 /* Co-Simulation */,
      fmuGUID,
      $"file://{FullFmuLibPath}/resources",
      functions.internalCallbackFunctions,
      visible,
      loggingOn);
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("Failed to create an FMU instance.");
    }
  }

  /*
  typedef fmi2Component fmi2InstantiateTYPE(
    fmi2String instanceName,
    fmi2Type fmuType,
    fmi2String fmuGUID,
    fmi2String fmuResourceLocation,
    const fmi2CallbackFunctions* functions,
    fmi2Boolean visible,
    fmi2Boolean loggingOn);
  */
  internal fmi2InstantiateTYPE fmi2Instantiate;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate IntPtr fmi2InstantiateTYPE(
    string instanceName,
    int fmuType,
    string fmuGUID,
    string fmuResourceLocation,
    Fmi2BindingCallbackFunctions.fmi2CallbackFunctions functions,
    bool visible,
    bool loggingOn);

  /*
    typedef void fmi2FreeInstanceTYPE(fmi2Component c);
  */
  internal fmi2FreeInstanceTYPE fmi2FreeInstance;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate void fmi2FreeInstanceTYPE(IntPtr c);

  public void SetupExperiment(double? tolerance, double startTime, double? stopTime)
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2SetupExperiment(
        component,
        tolerance.HasValue,
        (tolerance.HasValue) ? tolerance.Value : double.NaN,
        startTime,
        stopTime.HasValue,
        (stopTime.HasValue) ? stopTime.Value : double.NaN),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetupExperimentTYPE(
      fmi2Component c,
      fmi2Boolean toleranceDefined,
      fmi2Real tolerance,
      fmi2Real startTime,
      fmi2Boolean stopTimeDefined,
      fmi2Real stopTime);
  */
  internal fmi2SetupExperimentTYPE fmi2SetupExperiment;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2SetupExperimentTYPE(
    IntPtr c,
    bool toleranceDefined,
    double tolerance,
    double startTime,
    bool stopTimeDefined,
    double stopTime);

  public void EnterInitializationMode()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2EnterInitializationMode(component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2EnterInitializationModeTYPE(fmi2Component c);
  */
  internal fmi2EnterInitializationModeTYPE fmi2EnterInitializationMode;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2EnterInitializationModeTYPE(IntPtr c);

  public void ExitInitializationMode()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2ExitInitializationMode(component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2ExitInitializationModeTYPE(fmi2Component c);
  */
  internal fmi2ExitInitializationModeTYPE fmi2ExitInitializationMode;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2ExitInitializationModeTYPE(IntPtr c);


  public override void Terminate()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2Terminate(component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2TerminateTYPE(fmi2Component c);
  */
  internal fmi2TerminateTYPE fmi2Terminate;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2TerminateTYPE(IntPtr c);

  public override void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime)
  {
    // Note that this return code is special as the code '0' as well as '5' are success values
    var fmi2Status = fmi2DoStep(component, currentCommunicationPoint, communicationStepSize, out _);
    if (fmi2Status == 0)
    {
      // synchronous call - skip to new time retrieval
      // FMI 2 can never return early -> return calculated time
      lastSuccessfulTime = currentCommunicationPoint + communicationStepSize;
    }
    else if (fmi2Status == 5)
    {
      // doStep requires async handling -> wait 60s for doStep callback
      // TODO make timeout configurable
      if (waitForDoStepEvent.WaitOne(60000))
      {
        lastSuccessfulTime = currentCommunicationPoint + communicationStepSize;
      }
      else
      {
        // timeout logic
        throw new TimeoutException("SimTask did not return in time...");
      }
    }
    else
    {
      throw new InvalidOperationException($"fmi2Terminate exited with exit code '{fmi2Status}'.");
    }
  }

  /*
    typedef fmi2Status fmi2DoStepTYPE(fmi2Component c,
      fmi2Real currentCommunicationPoint,
      fmi2Real communicationStepSize,
      fmi2Boolean noSetFMUStatePriorToCurrentPoint);
  */
  internal fmi2DoStepTYPE fmi2DoStep;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2DoStepTYPE(
    IntPtr c,
    double currentCommunicationPoint,
    double communicationStepSize,
    out bool noSetFMUStatePriorToCurrentPoint);

  public void CancelStep()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2CancelStep(component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2CancelStepTYPE(fmi2Component c);
  */
  internal fmi2CancelStepTYPE fmi2CancelStep;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2CancelStepTYPE(IntPtr c);

  #endregion Common & Co-Simulation Functions for FMI 2.x

  #region Variable Getters & Setters

  /////////////
  // Getters //
  /////////////
  public double[] GetReal(uint[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new double[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2GetReal(component, valueReferences, (IntPtr)valueReferences.Length, result),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return result;
  }

  /*
    typedef fmi2Status fmi2GetRealTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2Real value[]);
  */
  internal fmi2GetRealTYPE fmi2GetReal;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2GetRealTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    double[] value);

  public int[] GetInteger(uint[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new int[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2GetInteger(component, valueReferences, (IntPtr)valueReferences.Length, result),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return result;
  }

  /*
    typedef fmi2Status fmi2GetIntegerTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2Integer value[]);
  */
  internal fmi2GetIntegerTYPE fmi2GetInteger;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2GetIntegerTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    int[] value);

  public bool[] GetBoolean(uint[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new bool[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2GetBoolean(component, valueReferences, (IntPtr)valueReferences.Length, result),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return result;
  }

  /*
    typedef fmi2Status fmi2GetBooleanTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2Boolean value[]);
  */
  internal fmi2GetBooleanTYPE fmi2GetBoolean;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2GetBooleanTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    bool[] value);

  public string[] GetString(uint[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var resultRaw = new IntPtr[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2GetString(component, valueReferences, (IntPtr)valueReferences.Length, resultRaw),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    var result = new string[resultRaw.Length];
    for (int i = 0; i < result.Length; i++)
    {
      var str = Marshal.PtrToStringUTF8(resultRaw[i]);
      if (str == null)
      {
        throw new BadImageFormatException("TODO");
      }
      result[i] = str;
    }

    return result;
  }

  /*
    typedef fmi2Status fmi2GetStringTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2String value[]);
  */
  internal fmi2GetStringTYPE fmi2GetString;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2GetStringTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    IntPtr[] value);

  /////////////
  // Setters //
  /////////////
  public void SetReal(uint[] valueReferences, double[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2SetReal(component, valueReferences, (IntPtr)valueReferences.Length, values),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetRealTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2Real value[]);
  */
  internal fmi2SetRealTYPE fmi2SetReal;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2SetRealTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    double[] value);

  public void SetInteger(uint[] valueReferences, int[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2SetInteger(component, valueReferences, (IntPtr)valueReferences.Length, values),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetIntegerTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2Integer value[]);
  */
  internal fmi2SetIntegerTYPE fmi2SetInteger;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2SetIntegerTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    int[] value);

  public void SetBoolean(uint[] valueReferences, bool[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    // bool cannot be cast trivially
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2SetBoolean(
        component,
        valueReferences,
        (IntPtr)valueReferences.Length,
        Array.ConvertAll(values, Convert.ToInt32)),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetBooleanTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2Boolean value[]);
  */
  internal fmi2SetBooleanTYPE fmi2SetBoolean;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2SetBooleanTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    int[] value);

  public void SetString(uint[] valueReferences, string[] values)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    IntPtr[] valuePtrs = new IntPtr[values.Length];
    for (int i = 0; i < values.Length; i++)
    {
      valuePtrs[i] = Marshal.StringToHGlobalAnsi(values[i]);
    }

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)fmi2SetString(component, valueReferences, (IntPtr)valueReferences.Length, valuePtrs),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
  typedef fmi2Status fmi2SetStringTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2String value[]);
  */
  internal fmi2SetStringTYPE fmi2SetString;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2SetStringTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    IntPtr[] valuePtrs);

  #endregion Variable Getters & Setters

  /*
   typedef fmi2Status fmi2GetFMUstateTYPE(fmi2Component c, fmi2FMUstate* FMUstate);
  */
  internal fmi2GetFMUstateTYPE fmi2GetFMUstate;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi2GetFMUstateTYPE(
    IntPtr c,
    IntPtr fmuState);


  public override FmiVersions GetFmiVersion()
  {
    return FmiVersions.Fmi2;
  }
}
