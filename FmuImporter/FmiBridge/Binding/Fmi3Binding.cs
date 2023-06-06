// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using System.Text;
using Fmi.Exceptions;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public delegate void fmi3LogMessageCallback(
  IntPtr instanceEnvironment,
  Fmi3Statuses status,
  string category,
  string message);

public enum Fmi3Statuses
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

public interface IFmi3Binding : IFmiBindingCommon
{
  public ModelDescription GetModelDescription();

  // Common & Co-Simulation Functions for FMI 3.0
  public void InstantiateCoSimulation(
    string instanceName,
    string instantiationToken,
    bool visible,
    bool loggingOn,
    fmi3LogMessageCallback logger);

  public void EnterInitializationMode(double? tolerance, double startTime, double? stopTime);
  public void ExitInitializationMode();

  public void SetDebugLogging(
    bool loggingOn,
    int nCategories,
    string[]? categories);

  // Getters & Setters
  public ReturnVariable GetFloat32(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetFloat64(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt8(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt8(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt16(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt16(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt32(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt32(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetInt64(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetUInt64(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetBoolean(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetString(fmi3ValueReference[] valueReferences);
  public ReturnVariable GetBinary(fmi3ValueReference[] valueReferences);
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
  private readonly GCHandle? loggerHandlerGcHandle;

#if OS_WINDOWS
  private const string osPath = "/binaries/x86_64-windows";
#elif OS_LINUX
  private const string osPath = "/binaries/x86_64-linux";
#elif OS_MAC
  private const string osPath = "/binaries/x86_64-darwin";
#endif

  public Fmi3Binding(string fmuPath) : base(fmuPath, osPath)
  {
    loggerHandlerGcHandle = null;

    // Common functions
    SetDelegate(out fmi3InstantiateCoSimulation);
    SetDelegate(out fmi3FreeInstance);
    SetDelegate(out fmi3EnterInitializationMode);
    SetDelegate(out fmi3ExitInitializationMode);
    SetDelegate(out fmi3DoStep);
    SetDelegate(out fmi3Terminate);
    SetDelegate(out fmi3SetDebugLogging);

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

#region IDisposable

  ~Fmi3Binding()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    fmi3FreeInstance(component);
    loggerHandlerGcHandle?.Free();
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
    fmi3LogMessageCallback logger)
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
      throw new NullReferenceException("Failed to create an FMU instance.");
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
    fmi3LogMessageCallback logMessage,
    IntPtr intermediateUpdate);

  /*
    typedef void fmi3FreeInstanceTYPE(fmi3Instance instance);
  */
  internal fmi3FreeInstanceTYPE fmi3FreeInstance;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate void fmi3FreeInstanceTYPE(IntPtr instance);

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


  public void SetDebugLogging(
    bool loggingOn,
    int nCategories,
    string[]? categories)
  {
    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetDebugLogging(component, loggingOn, (IntPtr)nCategories, categories),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi3Status fmi3SetDebugLoggingTYPE(
      fmi3Instance instance,
      fmi3Boolean loggingOn,
      size_t nCategories,
      const fmi3String categories[]);
  */
  internal fmi3SetDebugLoggingTYPE fmi3SetDebugLogging;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3SetDebugLoggingTYPE(
    IntPtr instance,
    bool loggingOn,
    size_t nCategories,
    string[]? categories);

  public override void GetValue(uint[] valueRefs, out ReturnVariable result, VariableTypes type)
  {
    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
      {
        result = GetFloat32(valueRefs);
        return;
      }
      case VariableTypes.Float64:
      {
        result = GetFloat64(valueRefs);
        return;
      }
      case VariableTypes.Int8:
      {
        result = GetInt8(valueRefs);
        return;
      }
      case VariableTypes.Int16:
      {
        result = GetInt16(valueRefs);
        return;
      }
      case VariableTypes.Int32:
      {
        result = GetInt32(valueRefs);
        return;
      }
      case VariableTypes.Int64:
      {
        result = GetInt64(valueRefs);
        return;
      }
      case VariableTypes.UInt8:
      {
        result = GetUInt8(valueRefs);
        return;
      }
      case VariableTypes.UInt16:
      {
        result = GetUInt16(valueRefs);
        return;
      }
      case VariableTypes.UInt32:
      {
        result = GetUInt32(valueRefs);
        return;
      }
      case VariableTypes.UInt64:
      {
        result = GetUInt64(valueRefs);
        return;
      }
      case VariableTypes.Boolean:
      {
        result = GetBoolean(valueRefs);
        return;
      }
      case VariableTypes.String:
      {
        result = GetString(valueRefs);
        return;
      }
      case VariableTypes.Binary:
      {
        result = GetBinary(valueRefs);
        return;
      }
      case VariableTypes.EnumFmi2:
        break;
      case VariableTypes.EnumFmi3:
      {
        result = GetInt64(valueRefs);
        return;
      }
      default:
        break;
    }

    throw new ArgumentOutOfRangeException(nameof(type), type, $"The type '{type}' is not supported.");
  }

  public override void SetValue(uint valueRef, byte[] data)
  {
    var mdVar = ModelDescription.Variables[valueRef];
    SetValue(mdVar, data);
  }

  internal override void SetValue(Variable mdVar, byte[] data)
  {
    var type = mdVar.VariableType;
    var isScalar = !(mdVar.Dimensions != null && mdVar.Dimensions.Length > 0);

    var arraySize = 1;
    if (!isScalar)
    {
      arraySize = BitConverter.ToInt32(data, 0);
      data = data.Skip(4).ToArray();
    }

    switch (type)
    {
      case VariableTypes.Undefined:
        break;
      case VariableTypes.Float32:
      {
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

        var unit = mdVar.TypeDefinition?.Unit;
        if (unit != null)
        {
          // Apply unit transformation
          // FMU value = [SIL Kit value] * factor + offset
          for (var i = 0; i < values.Length; i++)
          {
            var value = values[i];
            // first apply factor, then offset
            if (unit.Factor.HasValue)
            {
              value = Convert.ToSingle(value * unit.Factor.Value);
            }

            if (unit.Offset.HasValue)
            {
              value = Convert.ToSingle(value + unit.Offset.Value);
            }

            values[i] = value;
          }
        }

        SetFloat32(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.Float64:
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

        var unit = mdVar.TypeDefinition?.Unit;
        if (unit != null)
        {
          // Apply unit transformation
          // FMU value = [SIL Kit value] * factor + offset
          for (var i = 0; i < values.Length; i++)
          {
            var value = values[i];
            // first apply factor, then offset
            if (unit.Factor.HasValue)
            {
              value *= unit.Factor.Value;
            }

            if (unit.Offset.HasValue)
            {
              value += unit.Offset.Value;
            }

            values[i] = value;
          }
        }

        SetFloat64(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.Int8:
      {
        if (isScalar)
        {
          if (data.Length > 1)
          {
            throw new NotSupportedException("Unexpected size of data type.");
          }
        }

        var values = new sbyte[arraySize];
        Buffer.BlockCopy(data, 0, values, 0, data.Length);

        SetInt8(new[] { mdVar.ValueReference }, new[] { (sbyte)data[0] });
        return;
      }
      case VariableTypes.Int16:
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

        SetInt16(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.Int32:
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

        SetInt32(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.Int64:
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

        SetInt64(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.UInt8:
      {
        if (isScalar)
        {
          if (data.Length > 1)
          {
            throw new NotSupportedException("Unexpected size of data type.");
          }
        }

        SetUInt8(new[] { mdVar.ValueReference }, data);
        return;
      }
      case VariableTypes.UInt16:
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

        SetUInt16(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.UInt32:
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

        SetUInt32(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.UInt64:
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

        SetUInt64(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.Boolean:
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

        SetBoolean(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.String:
      {
        var values = new string[arraySize];

        // arrays are encoded as arrays of characters and thus always have a leading length indicator of 32 bit
        var dataOffset = 0;

        for (var i = 0; i < arraySize; i++)
        {
          var byteLength = BitConverter.ToInt32(data, dataOffset);
          dataOffset += 4; // 4 byte -> 32 bit
          var value = Encoding.UTF8.GetString(data, dataOffset, byteLength);
          dataOffset += byteLength;
          values[i] = value;
        }

        SetString(new[] { mdVar.ValueReference }, values);
        return;
      }
      case VariableTypes.Binary:
      {
        throw new NotSupportedException("Must be called with binSizes argument!");
      }
      case VariableTypes.EnumFmi2:
        break;
      case VariableTypes.EnumFmi3:
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

        SetInt64(new[] { mdVar.ValueReference }, values);
        return;
      }
      default:
        break;
    }

    throw new ArgumentOutOfRangeException();
  }

  public override void SetValue(uint valueRef, byte[] data, int[] binSizes)
  {
    var mdVar = ModelDescription.Variables[valueRef];
    var type = mdVar.VariableType;

    var isScalar = !(mdVar.Dimensions != null && mdVar.Dimensions.Length > 0);

    var arraySize = 1;
    if (!isScalar)
    {
      arraySize = BitConverter.ToInt32(data, 0);
      data = data.Skip(4).ToArray();

      if (binSizes.Sum() != data.Length)
      {
        throw new ArgumentOutOfRangeException(
          nameof(binSizes),
          $"The expected data length ({binSizes.Sum()}) " +
          $"does not match the received data length ({data.Length}).");
      }
    }

    if (type != VariableTypes.Binary)
    {
      throw new InvalidDataException("SetValue with binSizes must target a variable of type 'Binary'.");
    }

    var values = new IntPtr[arraySize];
    var handlers = new GCHandle[arraySize];

    var dataOffset = 0;
    for (var i = 0; i < arraySize; i++)
    {
      var currentBinary = new byte[binSizes[i]];
      Buffer.BlockCopy(data, dataOffset, currentBinary, 0, binSizes[i]);
      dataOffset += binSizes[i];

      var handler = GCHandle.Alloc(currentBinary, GCHandleType.Pinned);
      handlers[i] = handler;
      values[i] = handler.AddrOfPinnedObject();
    }

    SetBinary(new[] { valueRef }, new[] { (IntPtr)data.Length }, values);

    foreach (var gcHandle in handlers)
    {
      gcHandle.Free();
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
      Fmi.Helpers.Log(Fmi.Helpers.LogSeverity.Information, "FMU requested simulation termination.");
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
  public ReturnVariable GetFloat32(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new float[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetFloat32(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);


    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetFloat64(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new double[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetFloat64(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetInt8(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new sbyte[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt8(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetUInt8(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new byte[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt8(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetInt16(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new short[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt16(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetUInt16(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new ushort[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt16(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetInt32(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new int[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt32(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetUInt32(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new uint[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt32(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetInt64(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new long[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetInt64(
        component,
        valueReferences,
        (IntPtr)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetUInt64(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new ulong[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetUInt64(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetBoolean(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new bool[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetBoolean(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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

  public ReturnVariable GetString(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var resultRaw = new IntPtr[(int)nValues];

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3GetString(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        resultRaw,
        nValues),
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

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref modelDescription);
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
    IntPtr[] values,
    size_t nValues);

  public ReturnVariable GetBinary(fmi3ValueReference[] valueReferences)
  {
    if (component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var valueSizes = new size_t[valueReferences.Length];
    var nValues = CalculateValueLength(ref valueReferences);
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

    return ReturnVariable.CreateReturnVariable(
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

    var valuePtrs = new IntPtr[values.Length];
    for (var i = 0; i < values.Length; i++)
    {
      valuePtrs[i] = Marshal.StringToHGlobalAnsi(values[i]);
    }

    Helpers.ProcessReturnCode(
      (Fmi3Statuses)fmi3SetString(
        component,
        valueReferences,
        (size_t)valueReferences.Length,
        valuePtrs,
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
    IntPtr[] values,
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

  public override FmiVersions GetFmiVersion()
  {
    return FmiVersions.Fmi3;
  }
}
