// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using Fmi.Binding.Helper;
using Fmi.Exceptions;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

internal abstract class FmiBindingBase : IDisposable, IFmiBindingCommon
{
  private static readonly HashSet<int> sOkCodes = new HashSet<int>
  {
    (int)FmiStatus.OK,
    (int)FmiStatus.Warning,
    (int)FmiStatus.Pending // asynchronous doStep (FMI 2 only)
  };

  internal enum States
  {
    Initial,
    Instantiated,
    ConfigurationMode,
    InitializationMode,
    StepMode,
    Terminated,
    Freed
  }

  internal States CurrentState { get; set; } = States.Initial;

  public ModelDescription ModelDescription { get; }

  public string ExtractedFolderPath { get; }
  public string FullFmuLibPath { get; }

  private IntPtr DllPtr { set; get; }

  // ctor
  protected FmiBindingBase(string fmuPath, string osDependentPath)
  {
    ExtractedFolderPath = ModelLoader.ExtractFmu(fmuPath);

    ModelDescription = InitializeModelDescription(ExtractedFolderPath);

    FullFmuLibPath =
      $"{Path.GetFullPath(ExtractedFolderPath + osDependentPath + "/" + ModelDescription.CoSimulation.ModelIdentifier)}";
    InitializeModelBinding(FullFmuLibPath);
  }

#region IDisposable

  ~FmiBindingBase()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool _disposedValue;

  protected virtual void Dispose(bool disposing)
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
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

#endregion

  private ModelDescription InitializeModelDescription(string extractedFolderPath)
  {
    return ModelLoader.LoadModelFromExtractedPath(extractedFolderPath);
  }

  private void InitializeModelBinding(string fullPathToLibrary)
  {
    DllPtr = LoadFmiLibrary(fullPathToLibrary);
  }

  private IntPtr LoadFmiLibrary(string libraryPath)
  {
    IntPtr res;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      res = NativeMethods.LoadLibrary(libraryPath);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      res = NativeMethods.dlopen(libraryPath + ".so", 0x00002 /* RTLD_NOW */);
    }
    else
    {
      throw new NotSupportedException();
    }

    if (res == IntPtr.Zero)
    {
      throw new FileLoadException(
        $"Failed to retrieve a pointer from the provided FMU library. Path was '{libraryPath}'.");
    }

    return res;
  }

  protected void SetDelegate<T>(out T deleg)
    where T : Delegate
  {
    var delegateTypeName = typeof(T).Name;
    if (string.IsNullOrEmpty(delegateTypeName))
    {
      throw new FileLoadException("Failed to retrieve method name by reflection.");
    }

    delegateTypeName = delegateTypeName.Substring(0, delegateTypeName.Length - 4);

    IntPtr ptr;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      ptr = NativeMethods.GetProcAddress(DllPtr, delegateTypeName);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      ptr = NativeMethods.dlsym(DllPtr, delegateTypeName);
    }
    else
    {
      throw new NotSupportedException();
    }

    if (ptr == IntPtr.Zero)
    {
      throw new FileLoadException(
        $"Failed to retrieve function pointer to method '{delegateTypeName}'.");
    }

    deleg = (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
  }

  public abstract void GetValue(uint[] valueRefs, out ReturnVariable result, VariableTypes type);

  public abstract void SetValue(uint valueRef, byte[] data);
  internal abstract void SetValue(Variable mdVar, byte[] data);
  public abstract void SetValue(uint valueRef, byte[] data, int[] binSizes);

  public abstract void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime);

  public abstract void Terminate();

  public abstract void FreeInstance();

  public abstract FmiVersions GetFmiVersion();

  public void ProcessReturnCode(int statusCode, RuntimeMethodHandle? methodHandle)
  {
    var result = Common.Helpers.ProcessReturnCode(
      sOkCodes,
      statusCode,
      ((FmiStatus)statusCode).ToString(),
      methodHandle);
    if (result.Item1)
    {
      return;
    }

    if ((FmiStatus)statusCode is FmiStatus.Fatal)
    {
      // FMU is unrecoverable - consider it as freed
      CurrentState = States.Freed;
    }

    if ((FmiStatus)statusCode is FmiStatus.Discard or FmiStatus.Error)
    {
      // Errors before the initialized state lead to the terminated state, otherwise Terminate() is called
      if (CurrentState is States.Initial or States.Instantiated or States.InitializationMode)
      {
        CurrentState = States.Terminated;
      }
      else
      {
        // attempt to terminate FMU gracefully
        Terminate();
      }
    }

    try
    {
      Log(LogSeverity.Error, result.Item2!.ToString());
    }
    finally
    {
      // Throwing ensures that the FMU Importer will exit
      throw new NativeCallException(result.Item2?.ToString());
    }
  }

  protected void Log(LogSeverity severity, string message)
  {
    _loggerAction?.Invoke(LogSeverity.Error, message);
  }

  private Action<LogSeverity, string>? _loggerAction;

  public void SetLoggerCallback(Action<LogSeverity, string> callback)
  {
    _loggerAction = callback;
  }
}
