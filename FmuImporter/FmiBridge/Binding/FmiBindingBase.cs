// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using Fmi.Binding.Helper;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

internal abstract class FmiBindingBase : IDisposable, IFmiBindingCommon
{
  internal ModelDescription _modelDescription;

  public ModelDescription ModelDescription
  {
    get
    {
      return _modelDescription;
    }
    private set
    {
      _modelDescription = value;
    }
  }

  public string FullFmuLibPath { get; }

  private IntPtr DllPtr { set; get; }

  // ctor
  protected FmiBindingBase(string fmuPath, string osDependentPath)
  {
    var extractedFolderPath = ModelLoader.ExtractFmu(fmuPath);

    _modelDescription = InitializeModelDescription(extractedFolderPath);

    FullFmuLibPath =
      $"{Path.GetFullPath(extractedFolderPath + osDependentPath + "/" + ModelDescription.CoSimulation.ModelIdentifier)}";
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
      throw new FileLoadException("Failed to retrieve a pointer from the provided FMU library.");
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

  public abstract FmiVersions GetFmiVersion();
}
