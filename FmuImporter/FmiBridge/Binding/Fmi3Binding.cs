// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

// The commented out C Headers in this file belong to the source code of the FMI standard, which is licensed
// under the BSD 2-Clause license (SPDX-License-Identifier: BSD-2-Clause)
// Copyright (C) 2008-2011 MODELISAR consortium,
//               2012-2022 Modelica Association Project "FMI"
//               All rights reserved.

using System.Runtime.InteropServices;
using System.Text;
using Fmi.Binding.Helper;
using Fmi.Exceptions;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public delegate void Fmi3LogMessageCallback(
  IntPtr instanceEnvironment,
  FmiStatus status,
  string category,
  string message);

public static class Fmi3BindingFactory
{
  public static IFmi3Binding CreateFmi3Binding(string fmuPath)
  {
    return new Fmi3Binding(fmuPath);
  }
}

internal class Fmi3Binding : FmiBindingBase, IFmi3Binding
{
  private IntPtr _component;

  private static string OsPath
  {
    get
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        return "/binaries/x86_64-windows";
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        return "/binaries/x86_64-linux";
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        return "/binaries/x86_64-darwin";
      }

      throw new NotSupportedException();
    }
  }

  public Fmi3Binding(string fmuPath) : base(fmuPath, OsPath)
  {
    // Common functions
    SetDelegate(out _fmi3InstantiateCoSimulation);
    SetDelegate(out _fmi3FreeInstance);
    SetDelegate(out _fmi3EnterInitializationMode);
    SetDelegate(out _fmi3ExitInitializationMode);
    SetDelegate(out _fmi3DoStep);
    SetDelegate(out _fmi3Terminate);
    SetDelegate(out _fmi3SetDebugLogging);

    // Variable Getters and Setters
    SetDelegate(out _fmi3GetFloat32);
    SetDelegate(out _fmi3GetFloat64);
    SetDelegate(out _fmi3GetInt8);
    SetDelegate(out _fmi3GetUInt8);
    SetDelegate(out _fmi3GetInt16);
    SetDelegate(out _fmi3GetUInt16);
    SetDelegate(out _fmi3GetInt32);
    SetDelegate(out _fmi3GetUInt32);
    SetDelegate(out _fmi3GetInt64);
    SetDelegate(out _fmi3GetUInt64);
    SetDelegate(out _fmi3GetBoolean);
    SetDelegate(out _fmi3GetString);
    SetDelegate(out _fmi3GetBinary);
    SetDelegate(out _fmi3SetFloat32);
    SetDelegate(out _fmi3SetFloat64);
    SetDelegate(out _fmi3SetInt8);
    SetDelegate(out _fmi3SetUInt8);
    SetDelegate(out _fmi3SetInt16);
    SetDelegate(out _fmi3SetUInt16);
    SetDelegate(out _fmi3SetInt32);
    SetDelegate(out _fmi3SetUInt32);
    SetDelegate(out _fmi3SetInt64);
    SetDelegate(out _fmi3SetUInt64);
    SetDelegate(out _fmi3SetBoolean);
    SetDelegate(out _fmi3SetString);
    SetDelegate(out _fmi3SetBinary);
  }

#region IDisposable

  ~Fmi3Binding()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    if (CurrentState != States.Freed)
    {
      try
      {
        FreeInstance();
      }
      catch
      {
        Helpers.Log(
          Helpers.LogSeverity.Debug,
          $"FreeInstance in {GetType().FullName}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name ?? "??"} " +
          $"threw an exception, which was ignored, because the Importer is currently exiting.");
      }
    }
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

  public ModelDescription GetModelDescription()
  {
    return ModelDescription;
  }

#region Common & Co-Simulation Functions for FMI 3.0

  private Fmi3LogMessageCallback? _logger;
  private IntPtr _loggerPtr;

  public void InstantiateCoSimulation(
    string instanceName,
    string instantiationToken,
    bool visible,
    bool loggingOn,
    Fmi3LogMessageCallback logger)
  {
    var resourcePath = new DirectoryInfo(
      ExtractedFolderPath + $"{Path.DirectorySeparatorChar}resources{Path.DirectorySeparatorChar}").FullName;

    _logger = logger;

    _loggerPtr = Marshal.GetFunctionPointerForDelegate(logger);

    _component = _fmi3InstantiateCoSimulation(
      instanceName,
      instantiationToken,
      resourcePath,
      visible,
      loggingOn,
      false,
      false,
      IntPtr.Zero,
      IntPtr.Zero,
      IntPtr.Zero,
      _loggerPtr,
      IntPtr.Zero);
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("Failed to create an FMU instance.");
    }

    CurrentState = States.Instantiated;
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
  private readonly fmi3InstantiateCoSimulationTYPE _fmi3InstantiateCoSimulation;

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
    IntPtr logMessage,
    IntPtr intermediateUpdate);

  /*
    typedef void fmi3FreeInstanceTYPE(fmi3Instance instance);
  */
  private readonly fmi3FreeInstanceTYPE _fmi3FreeInstance;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate void fmi3FreeInstanceTYPE(IntPtr instance);

  public void EnterInitializationMode(double? tolerance, double startTime, double? stopTime)
  {
    ProcessReturnCode(
      _fmi3EnterInitializationMode(
        _component,
        tolerance.HasValue,
        (tolerance.HasValue) ? tolerance.Value : double.NaN,
        startTime,
        stopTime.HasValue,
        (stopTime.HasValue) ? stopTime.Value : double.NaN),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    CurrentState = States.InitializationMode;
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
  private readonly fmi3EnterInitializationModeTYPE _fmi3EnterInitializationMode;

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
    ProcessReturnCode(
      _fmi3ExitInitializationMode(_component),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    CurrentState = States.StepMode;
  }

  /*
    typedef fmi3Status fmi3ExitInitializationModeTYPE(fmi3Instance instance);
   */
  private readonly fmi3ExitInitializationModeTYPE _fmi3ExitInitializationMode;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3ExitInitializationModeTYPE(IntPtr instance);


  public void SetDebugLogging(
    bool loggingOn,
    int nCategories,
    string[]? categories)
  {
    ProcessReturnCode(
      _fmi3SetDebugLogging(_component, loggingOn, (IntPtr)nCategories, categories),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
    typedef fmi3Status fmi3SetDebugLoggingTYPE(
      fmi3Instance instance,
      fmi3Boolean loggingOn,
      size_t nCategories,
      const fmi3String categories[]);
  */
  private readonly fmi3SetDebugLoggingTYPE _fmi3SetDebugLogging;

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
    var isScalar = mdVar.IsScalar;

    var arraySize = isScalar ? 1 : (int)mdVar.FlattenedArrayLength;

    switch (type)
    {
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

    var isScalar = mdVar.IsScalar;

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
    ProcessReturnCode(
      _fmi3DoStep(
        _component,
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
      Helpers.Log(Helpers.LogSeverity.Information, "FMU requested simulation termination.");
      Terminate();
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
  private readonly fmi3DoStepTYPE _fmi3DoStep;

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
    if (CurrentState == States.Terminated)
    {
      // skip termination
      return;
    }

    try
    {
      ProcessReturnCode(
        _fmi3Terminate(_component),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    finally
    {
      CurrentState = States.Terminated;
    }
  }

  /*
    typedef fmi3Status fmi3TerminateTYPE(fmi3Instance instance);
   */
  private readonly fmi3TerminateTYPE _fmi3Terminate;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3TerminateTYPE(IntPtr instance);

  public override void FreeInstance()
  {
    _fmi3FreeInstance(_component);
    CurrentState = States.Freed;
  }

#endregion Common & Co-Simulation Functions for FMI 3.0

#region Getters & Setters

  /////////////
  // Getters //
  /////////////
  public ReturnVariable GetFloat32(fmi3ValueReference[] valueReferences)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new float[(int)nValues];

    ProcessReturnCode(
      _fmi3GetFloat32(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);


    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetFloat32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Float32 values[],
      size_t nValues);
  */
  private readonly fmi3GetFloat32TYPE _fmi3GetFloat32;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new double[(int)nValues];

    ProcessReturnCode(
      _fmi3GetFloat64(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetFloat64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Float64 values[],
      size_t nValues);
  */
  private readonly fmi3GetFloat64TYPE _fmi3GetFloat64;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new sbyte[(int)nValues];

    ProcessReturnCode(
      _fmi3GetInt8(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetInt8TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int8 values[],
      size_t nValues);
  */
  private readonly fmi3GetInt8TYPE _fmi3GetInt8;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new byte[(int)nValues];

    ProcessReturnCode(
      _fmi3GetUInt8(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetUInt8TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt8 values[],
      size_t nValues);
  */
  private readonly fmi3GetUInt8TYPE _fmi3GetUInt8;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new short[(int)nValues];

    ProcessReturnCode(
      _fmi3GetInt16(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetInt16TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int16 values[],
      size_t nValues);
  */
  private readonly fmi3GetInt16TYPE _fmi3GetInt16;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new ushort[(int)nValues];

    ProcessReturnCode(
      _fmi3GetUInt16(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetUInt16TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt16 values[],
      size_t nValues);
  */
  private readonly fmi3GetUInt16TYPE _fmi3GetUInt16;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new int[(int)nValues];

    ProcessReturnCode(
      _fmi3GetInt32(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetInt32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int32 values[],
      size_t nValues);
  */
  private readonly fmi3GetInt32TYPE _fmi3GetInt32;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new uint[(int)nValues];

    ProcessReturnCode(
      _fmi3GetUInt32(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetUInt32TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt32 values[],
      size_t nValues);
  */
  private readonly fmi3GetUInt32TYPE _fmi3GetUInt32;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new long[(int)nValues];

    ProcessReturnCode(
      _fmi3GetInt64(
        _component,
        valueReferences,
        (IntPtr)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetInt64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Int64 values[],
      size_t nValues);
  */
  private readonly fmi3GetInt64TYPE _fmi3GetInt64;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var result = new ulong[(int)nValues];

    ProcessReturnCode(
      _fmi3GetUInt64(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        result,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetUInt64TYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3UInt64 values[],
      size_t nValues);
  */
  private readonly fmi3GetUInt64TYPE _fmi3GetUInt64;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    // bools are not blittable -> retrieve them as IntPtr and convert result afterwards
    var tmpResult = new IntPtr[(int)nValues];

    ProcessReturnCode(
      _fmi3GetBoolean(
        _component,
        valueReferences,
        (size_t)valueReferences.Length,
        tmpResult,
        nValues),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);


    var result = Array.ConvertAll(tmpResult, e => e != IntPtr.Zero);
    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetBooleanTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3Boolean values[],
      size_t nValues);
  */
  private readonly fmi3GetBooleanTYPE _fmi3GetBoolean;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
  internal delegate int fmi3GetBooleanTYPE(
    IntPtr instance,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
    fmi3ValueReference[] valueReferences,
    size_t nValueReferences,
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
    IntPtr[] values,
    size_t nValues);

  public ReturnVariable GetString(fmi3ValueReference[] valueReferences)
  {
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var nValues = CalculateValueLength(ref valueReferences);
    var resultRaw = new IntPtr[(int)nValues];

    ProcessReturnCode(
      _fmi3GetString(
        _component,
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

    return ReturnVariable.CreateReturnVariable(valueReferences, result, nValues, ref _modelDescription);
  }

  /*
    typedef fmi3Status fmi3GetStringTYPE(
      fmi3Instance instance,
      const fmi3ValueReference valueReferences[],
      size_t nValueReferences,
      fmi3String values[],
      size_t nValues);
  */
  private readonly fmi3GetStringTYPE _fmi3GetString;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var valueSizes = new size_t[valueReferences.Length];
    var nValues = CalculateValueLength(ref valueReferences);
    var result = new fmi3Binary[(int)nValues];

    ProcessReturnCode(
      _fmi3GetBinary(
        _component,
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
      ref _modelDescription,
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
  private readonly fmi3GetBinaryTYPE _fmi3GetBinary;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetFloat32(
        _component,
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
  private readonly fmi3SetFloat32TYPE _fmi3SetFloat32;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetFloat64(
        _component,
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
  private readonly fmi3SetFloat64TYPE _fmi3SetFloat64;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetInt8(
        _component,
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
  private readonly fmi3SetInt8TYPE _fmi3SetInt8;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetUInt8(
        _component,
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
  private readonly fmi3SetUInt8TYPE _fmi3SetUInt8;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetInt16(
        _component,
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
  private readonly fmi3SetInt16TYPE _fmi3SetInt16;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetUInt16(
        _component,
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
  private readonly fmi3SetUInt16TYPE _fmi3SetUInt16;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetInt32(
        _component,
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
  private readonly fmi3SetInt32TYPE _fmi3SetInt32;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetUInt32(
        _component,
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
  private readonly fmi3SetUInt32TYPE _fmi3SetUInt32;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetInt64(
        _component,
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
  private readonly fmi3SetInt64TYPE _fmi3SetInt64;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetUInt64(
        _component,
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
  private readonly fmi3SetUInt64TYPE _fmi3SetUInt64;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetBoolean(
        _component,
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
  private readonly fmi3SetBooleanTYPE _fmi3SetBoolean;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    var valuePtrs = new IntPtr[values.Length];
    for (var i = 0; i < values.Length; i++)
    {
      valuePtrs[i] = Marshal.StringToHGlobalAnsi(values[i]);
    }

    ProcessReturnCode(
      _fmi3SetString(
        _component,
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
  private readonly fmi3SetStringTYPE _fmi3SetString;

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
    if (_component == IntPtr.Zero)
    {
      throw new NullReferenceException("FMU was not initialized.");
    }

    ProcessReturnCode(
      _fmi3SetBinary(
        _component,
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
  private readonly fmi3SetBinaryTYPE _fmi3SetBinary;

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
