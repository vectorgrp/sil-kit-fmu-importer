using System.Runtime.InteropServices;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public interface IFmiBindingCommon : IDisposable
{
  public void GetValue(uint[] valueRefs, out ReturnVariable result, Type type);

  public void SetValue(uint valueRef, byte[] data);
  public void SetValue(uint valueRef, byte[] data, int[] binSizes);

  public void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime);

  public void Terminate();

  public FmiVersions GetFmiVersion();
}

internal abstract class FmiBindingBase : IDisposable, IFmiBindingCommon
{
  public ModelDescription ModelDescription
  {
    get
    {
      return modelDescription;
    }
    private set
    {
      modelDescription = value;
    }
  }

  public string FullFmuLibPath { get; }

  private IntPtr DllPtr { set; get; }

  // ctor
  protected FmiBindingBase(string fmuPath, string osDependentPath)
  {
    var extractedFolderPath = ModelLoader.ExtractFmu(fmuPath);

    InitializeModelDescription(extractedFolderPath);
    if (ModelDescription == null)
    {
      throw new NullReferenceException("Failed to initialize model description.");
    }

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

  private bool mDisposedValue;
  protected ModelDescription modelDescription = null!;

  protected virtual void Dispose(bool disposing)
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
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

#endregion

  private void InitializeModelDescription(string extractedFolderPath)
  {
    ModelDescription = ModelLoader.LoadModelFromExtractedPath(extractedFolderPath);
  }

  private void InitializeModelBinding(string fullPathToLibrary)
  {
    DllPtr = LoadFmiLibrary(fullPathToLibrary);
  }


  private IntPtr LoadFmiLibrary(string libraryPath)
  {
#if OS_WINDOWS
    var res = NativeMethods.LoadLibrary(libraryPath);
#elif (OS_LINUX || OS_MAC)
    var res = NativeMethods.dlopen(libraryPath + ".so", 0x00002 /* RTLD_NOW */);
#else
    throw new NotSupportedException();
#endif
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


#if OS_WINDOWS
    var ptr = NativeMethods.GetProcAddress(DllPtr, delegateTypeName);
#elif (OS_LINUX || OS_MAC)
    var ptr = NativeMethods.dlsym(DllPtr, delegateTypeName);
#else
    throw new NotSupportedException();
#endif
    if (ptr == IntPtr.Zero)
    {
      throw new FileLoadException(
        $"Failed to retrieve function pointer to method '{delegateTypeName}'.");
    }

    deleg = (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
  }

  public abstract void GetValue(uint[] valueRefs, out ReturnVariable result, Type type);

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
