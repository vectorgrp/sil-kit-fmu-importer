﻿// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using System.Text;
using Fmi.Binding.Helper;
using Fmi.Exceptions;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public struct Fmi2BindingCallbackFunctions
{
  public Fmi2BindingCallbackFunctions(Logger loggerDelegate, StepFinished stepFinishedDelegate)
  {
    InternalCallbackFunctions = new Fmi2CallbackFunctions()
    {
      logger = (_, name, status, category, message) =>
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
      stepFinished = (_, status) =>
      {
        Fmi.Helpers.Log(Fmi.Helpers.LogSeverity.Debug, "Step finished with status = " + status);
        stepFinishedDelegate((Fmi2Statuses)status);
      },
      componentEnvironment = IntPtr.Zero
    };
  }

  public delegate void Logger(
    string instanceName,
    Fmi2Statuses status,
    string category,
    string message);

  public delegate void StepFinished(Fmi2Statuses status);

  internal Fmi2CallbackFunctions InternalCallbackFunctions;

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

  internal delegate IntPtr Fmi2CallbackAllocateMemory(IntPtr /*size_t*/ nobj, IntPtr /*size_t*/ size);

  internal delegate void Fmi2CallbackFreeMemory(IntPtr nobj);

  internal delegate void Fmi2StepFinished(IntPtr componentEnvironment, int status);

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal struct Fmi2CallbackFunctions
  {
    internal fmi2CallbackLogger logger;
    internal Fmi2CallbackAllocateMemory allocateMemory;
    internal Fmi2CallbackFreeMemory freeMemory;
    internal Fmi2StepFinished stepFinished;
    internal IntPtr componentEnvironment;
  }
}

public enum Fmi2Statuses
{
  OK,
  Warning,
  Discard,
  Error,
  Fatal,
  Pending
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
  private IntPtr _component = IntPtr.Zero;

#if OS_WINDOWS
  private const string OsPath = "/binaries/win64";
#elif OS_LINUX
  private const string OsPath = "/binaries/linux64";
#elif OS_MAC
  private const string OsPath = "/binaries/darwin64";
#endif

  private static readonly AutoResetEvent WaitForDoStepEvent = new AutoResetEvent(false);

  public Fmi2Binding(string fmuPath) : base(fmuPath, OsPath)
  {
    // Common Functions
    SetDelegate(out _fmi2SetDebugLogging);
    SetDelegate(out _fmi2Instantiate);
    SetDelegate(out _fmi2FreeInstance);
    SetDelegate(out _fmi2SetupExperiment);
    SetDelegate(out _fmi2EnterInitializationMode);
    SetDelegate(out _fmi2ExitInitializationMode);
    SetDelegate(out _fmi2Terminate);

    // Functions for FMI2 for Co-Simulation
    SetDelegate(out _fmi2DoStep);
    SetDelegate(out _fmi2CancelStep);

    // Variable Getters and Setters
    SetDelegate(out _fmi2GetReal);
    SetDelegate(out _fmi2GetInteger);
    SetDelegate(out _fmi2GetString);
    SetDelegate(out _fmi2GetBoolean);
    SetDelegate(out _fmi2SetReal);
    SetDelegate(out _fmi2SetInteger);
    SetDelegate(out _fmi2SetString);
    SetDelegate(out _fmi2SetBoolean);
  }

#region IDisposable

  ~Fmi2Binding()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    _fmi2FreeInstance(_component);
  }

  private bool _disposedValue;

  protected override void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
      }

      ReleaseUnmanagedResources();
      _disposedValue = true;
    }

    base.Dispose(disposing);
  }

#endregion IDisposable

  public override void GetValue(uint[] valueRefs, out ReturnVariable result, VariableTypes type)
  {
    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
        break;
      case VariableTypes.Float64:
      {
        var vFmi = GetReal(valueRefs);
        result = ReturnVariable.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref _modelDescription);
        return;
      }
      case VariableTypes.Int8:
        break;
      case VariableTypes.Int16:
        break;
      case VariableTypes.Int32:
      {
        var vFmi = GetInteger(valueRefs);
        result = ReturnVariable.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref _modelDescription);
        return;
      }
      case VariableTypes.Int64:
        break;
      case VariableTypes.UInt8:
        break;
      case VariableTypes.UInt16:
        break;
      case VariableTypes.UInt32:
        break;
      case VariableTypes.UInt64:
        break;
      case VariableTypes.Boolean:
      {
        var vFmi = GetBoolean(valueRefs);
        result = ReturnVariable.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref _modelDescription);
        return;
      }
      case VariableTypes.String:
      {
        var vFmi = GetString(valueRefs);
        result = ReturnVariable.CreateReturnVariable(valueRefs, vFmi, valueRefs.Length, ref _modelDescription);
        return;
      }
      case VariableTypes.Binary:
        break;
      case VariableTypes.EnumFmi2:
      {
        var vFmi = GetInteger(valueRefs);
        // FMU Importer handles enums as Int64, not as Int32
        var convertedVFmi = Array.ConvertAll(vFmi, Convert.ToInt64);
        result = ReturnVariable.CreateReturnVariable(valueRefs, convertedVFmi, valueRefs.Length, ref _modelDescription);
        return;
      }
      case VariableTypes.EnumFmi3:
        break;
      default:
        break;
    }

    throw new ArgumentOutOfRangeException(
      nameof(type),
      type,
      $"The provided type '{type}' is not supported in FMI 2.0");
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

    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
        break;
      case VariableTypes.Float64:
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
        return;
      }
      case VariableTypes.Int8:
        break;
      case VariableTypes.Int16:
        break;
      case VariableTypes.Int32:
      {
        var value = BitConverter.ToInt32(data);
        SetInteger(new[] { mdVar.ValueReference }, new[] { value });
        return;
      }
      case VariableTypes.Int64:
        break;
      case VariableTypes.UInt8:
        break;
      case VariableTypes.UInt16:
        break;
      case VariableTypes.UInt32:
        break;
      case VariableTypes.UInt64:
        break;
      case VariableTypes.Boolean:
      {
        var value = BitConverter.ToBoolean(data);
        SetBoolean(new[] { mdVar.ValueReference }, new[] { value });
        return;
      }
      case VariableTypes.String:
      {
        var byteLength = BitConverter.ToInt32(data, 0);
        // offset = 4 byte -> 32 bit
        var value = Encoding.UTF8.GetString(data, 4, byteLength);
        SetString(new[] { mdVar.ValueReference }, new[] { value });
        return;
      }
      case VariableTypes.Binary:
        break;
      case VariableTypes.EnumFmi2:
      {
        var value = BitConverter.ToInt64(data);
        if (value > Int32.MaxValue)
        {
          throw new InvalidOperationException(
            $"Failed to set enumerator for variable '{mdVar.Name}'. " +
            $"Reason: The value is too large (> Int32) and therefore cannot be handled by FMI 2.0 FMUs.");
        }

        SetInteger(new[] { mdVar.ValueReference }, new[] { (int)value });
        return;
      }
      case VariableTypes.EnumFmi3:
        break;
      default:
        break;
    }

    throw new ArgumentOutOfRangeException();
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
    WaitForDoStepEvent.Set();
  }

#region Common & Co-Simulation Functions for FMI 2.x

  public void SetDebugLogging(bool loggingOn, string[] categories)
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2SetDebugLogging(_component, loggingOn, (IntPtr)categories.Length, categories),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
  typedef fmi2Status  fmi2SetDebugLoggingTYPE(
    fmi2Component c,
    fmi2Boolean loggingOn,
    size_t nCategories,
    const fmi2String categories[]);
  */
  private readonly fmi2SetDebugLoggingTYPE _fmi2SetDebugLogging;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2SetDebugLoggingTYPE(
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
    _component = _fmi2Instantiate(
      instanceName,
      1 /* Co-Simulation */,
      fmuGUID,
      $"file://{FullFmuLibPath}/resources",
      functions.InternalCallbackFunctions,
      visible,
      loggingOn);
    if (_component == IntPtr.Zero)
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
  private readonly fmi2InstantiateTYPE _fmi2Instantiate;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate IntPtr fmi2InstantiateTYPE(
    string instanceName,
    int fmuType,
    string fmuGUID,
    string fmuResourceLocation,
    Fmi2BindingCallbackFunctions.Fmi2CallbackFunctions functions,
    bool visible,
    bool loggingOn);

  /*
    typedef void fmi2FreeInstanceTYPE(fmi2Component c);
  */
  private readonly fmi2FreeInstanceTYPE _fmi2FreeInstance;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate void fmi2FreeInstanceTYPE(IntPtr c);

  public void SetupExperiment(double? tolerance, double startTime, double? stopTime)
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2SetupExperiment(
        _component,
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
  private readonly fmi2SetupExperimentTYPE _fmi2SetupExperiment;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2SetupExperimentTYPE(
    IntPtr c,
    bool toleranceDefined,
    double tolerance,
    double startTime,
    bool stopTimeDefined,
    double stopTime);

  public void EnterInitializationMode()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2EnterInitializationMode(_component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2EnterInitializationModeTYPE(fmi2Component c);
  */
  private readonly fmi2EnterInitializationModeTYPE _fmi2EnterInitializationMode;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2EnterInitializationModeTYPE(IntPtr c);

  public void ExitInitializationMode()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2ExitInitializationMode(_component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2ExitInitializationModeTYPE(fmi2Component c);
  */
  private readonly fmi2ExitInitializationModeTYPE _fmi2ExitInitializationMode;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2ExitInitializationModeTYPE(IntPtr c);


  public override void Terminate()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2Terminate(_component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2TerminateTYPE(fmi2Component c);
  */
  private readonly fmi2TerminateTYPE _fmi2Terminate;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2TerminateTYPE(IntPtr c);

  public override void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime)
  {
    // Note that this return code is special as the code '0' as well as '5' are success values
    var fmi2Status = _fmi2DoStep(_component, currentCommunicationPoint, communicationStepSize, out _);
    if (fmi2Status == 0)
    {
      // synchronous call - skip to new time retrieval
      // FMI 2 can never return early -> return calculated time
      lastSuccessfulTime = currentCommunicationPoint + communicationStepSize;
    }
    else if (fmi2Status == 5)
    {
      // doStep requires async handling
      WaitForDoStepEvent.WaitOne();
      lastSuccessfulTime = currentCommunicationPoint + communicationStepSize;
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
  private readonly fmi2DoStepTYPE _fmi2DoStep;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2DoStepTYPE(
    IntPtr c,
    double currentCommunicationPoint,
    double communicationStepSize,
    out bool noSetFMUStatePriorToCurrentPoint);

  public void CancelStep()
  {
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2CancelStep(_component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2CancelStepTYPE(fmi2Component c);
  */
  private readonly fmi2CancelStepTYPE _fmi2CancelStep;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2CancelStepTYPE(IntPtr c);

#endregion Common & Co-Simulation Functions for FMI 2.x

#region Variable Getters & Setters

  /////////////
  // Getters //
  /////////////
  public double[] GetReal(uint[] valueReferences)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new double[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2GetReal(_component, valueReferences, (IntPtr)valueReferences.Length, result),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return result;
  }

  /*
    typedef fmi2Status fmi2GetRealTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2Real value[]);
  */
  private readonly fmi2GetRealTYPE _fmi2GetReal;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2GetRealTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    double[] value);

  public int[] GetInteger(uint[] valueReferences)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new int[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2GetInteger(_component, valueReferences, (IntPtr)valueReferences.Length, result),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return result;
  }

  /*
    typedef fmi2Status fmi2GetIntegerTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2Integer value[]);
  */
  private readonly fmi2GetIntegerTYPE _fmi2GetInteger;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2GetIntegerTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    int[] value);

  public bool[] GetBoolean(uint[] valueReferences)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var result = new bool[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2GetBoolean(_component, valueReferences, (IntPtr)valueReferences.Length, result),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return result;
  }

  /*
    typedef fmi2Status fmi2GetBooleanTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2Boolean value[]);
  */
  private readonly fmi2GetBooleanTYPE _fmi2GetBoolean;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2GetBooleanTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    bool[] value);

  public string[] GetString(uint[] valueReferences)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var resultRaw = new IntPtr[valueReferences.Length];

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2GetString(_component, valueReferences, (IntPtr)valueReferences.Length, resultRaw),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    var result = new string[resultRaw.Length];
    for (var i = 0; i < result.Length; i++)
    {
      var str = Marshal.PtrToStringUTF8(resultRaw[i]);
      if (str == null)
      {
        throw new NativeCallException(
          $"Failed to retrieve data via {System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "(unknown method)"}.");
      }

      result[i] = str;
    }

    return result;
  }

  /*
    typedef fmi2Status fmi2GetStringTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, fmi2String value[]);
  */
  private readonly fmi2GetStringTYPE _fmi2GetString;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2GetStringTYPE(
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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2SetReal(_component, valueReferences, (IntPtr)valueReferences.Length, values),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetRealTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2Real value[]);
  */
  private readonly fmi2SetRealTYPE _fmi2SetReal;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2SetRealTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    double[] value);

  public void SetInteger(uint[] valueReferences, int[] values)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2SetInteger(_component, valueReferences, (IntPtr)valueReferences.Length, values),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetIntegerTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2Integer value[]);
  */
  private readonly fmi2SetIntegerTYPE _fmi2SetInteger;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2SetIntegerTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    int[] value);

  public void SetBoolean(uint[] valueReferences, bool[] values)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    // bool cannot be cast trivially
    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2SetBoolean(
        _component,
        valueReferences,
        (IntPtr)valueReferences.Length,
        Array.ConvertAll(values, Convert.ToInt32)),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi2Status fmi2SetBooleanTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2Boolean value[]);
  */
  private readonly fmi2SetBooleanTYPE _fmi2SetBoolean;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2SetBooleanTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    int[] value);

  public void SetString(uint[] valueReferences, string[] values)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var valuePtrs = new IntPtr[values.Length];
    for (var i = 0; i < values.Length; i++)
    {
      valuePtrs[i] = Marshal.StringToHGlobalAnsi(values[i]);
    }

    Helpers.ProcessReturnCode(
      (Fmi2Statuses)_fmi2SetString(_component, valueReferences, (IntPtr)valueReferences.Length, valuePtrs),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
  typedef fmi2Status fmi2SetStringTYPE(fmi2Component c, const fmi2ValueReference vr[], size_t nvr, const fmi2String value[]);
  */
  private readonly fmi2SetStringTYPE _fmi2SetString;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  private delegate int fmi2SetStringTYPE(
    IntPtr c,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    uint[] /*fmi2ValueReference[]*/ vr,
    IntPtr nvr,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    IntPtr[] valuePtrs);

#endregion Variable Getters & Setters

  public override FmiVersions GetFmiVersion()
  {
    return FmiVersions.Fmi2;
  }
}
