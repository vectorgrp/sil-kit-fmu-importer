﻿// SPDX-License-Identifier: MIT
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


  public InternalFmuStates CurrentState { get; internal set; } = InternalFmuStates.Initial;

  public ModelDescription ModelDescription { get; }

  private readonly string _extractedFolderPath;

  public string ExtractedFolderPath
  {
    get
    {
      return _extractedFolderPath;
    }
  }

  public string FullFmuLibPath { get; }

  private IntPtr DllPtr { set; get; }

  private bool _isTemporary;

  internal bool IsTemporary
  {
    get
    {
      return _isTemporary;
    }
    set
    {
      _isTemporary = value;
    }
  }

  // ctor
  protected FmiBindingBase(string fmuPath, string osDependentPath, Action<LogSeverity, string> logCallback)
  {
    _loggerAction = logCallback;
    ModelLoader.ExtractFmu(fmuPath, out _extractedFolderPath, out _isTemporary);
    if (IsTemporary)
    {
      Log(LogSeverity.Debug, $"Temporarily extracted the FMU to '{_extractedFolderPath}'.");
    }

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
    var failCounter = 10;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      while (NativeMethods.FreeLibrary(DllPtr) && failCounter > 0)
      {
        --failCounter;
      }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      while (NativeMethods.dlclose(DllPtr) != 0 && failCounter > 0)
      {
        --failCounter;
      }
    }
    else
    {
      throw new NotSupportedException();
    }

    // if DLL was freed successfully and FMU was extracted to temporary folder, delete that folder and its content
    if (failCounter > 0 && IsTemporary)
    {
      var dir = Directory.GetParent(ExtractedFolderPath) ?? new DirectoryInfo(ExtractedFolderPath);
      dir.Delete(true);
    }
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
    try
    {
      return ModelLoader.LoadModelFromExtractedPath(extractedFolderPath);
    }
    catch (Exception e)
    {
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.FailedToReadModelDescription;
      }

      Log(LogSeverity.Error, $"An error was encountered while reading the model description: {e.Message}");
      Log(LogSeverity.Debug, $"An error was encountered while reading the model description: {e}");
      throw;
    }
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
      if (result.Item2 != null)
      {
        Log(LogSeverity.Warning, result.Item2.ToString());
      }

      return;
    }

    if ((FmiStatus)statusCode is FmiStatus.Fatal)
    {
      // FMU is unrecoverable - consider it as freed
      CurrentState = InternalFmuStates.Freed;
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.FmuFailedWithFatal;
      }
    }

    if ((FmiStatus)statusCode is FmiStatus.Error)
    {
      // the FMU is internally terminated, the Importer needs to 'free' the FMU
      CurrentState = InternalFmuStates.TerminatedWithError;
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.FmuFailedWithError;
      }

      FreeInstance();
    }

    if ((FmiStatus)statusCode is FmiStatus.Discard)
    {
      // We treat 'Discard' the same way as 'Error' (as suggested by the standard)
      CurrentState = InternalFmuStates.TerminatedWithError;
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.FmuFailedWithDiscard;
      }

      FreeInstance();
    }

    // Throwing ensures that the FMU Importer will exit
    throw new NativeCallException(result.Item2?.ToString());
  }

  protected void Log(LogSeverity severity, string message)
  {
    _loggerAction?.Invoke(severity, message);
  }

  private readonly Action<LogSeverity, string>? _loggerAction;
}
