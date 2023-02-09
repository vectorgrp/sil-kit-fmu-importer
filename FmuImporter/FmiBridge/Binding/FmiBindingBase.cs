using System.Runtime.InteropServices;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public interface IFmiBindingCommon
{
  public void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime);

  public void Terminate();
}

internal abstract class FmiBindingBase : IDisposable, IFmiBindingCommon
{
  public ModelDescription ModelDescription { get; private set; }

  public string FullFmuLibraryPath { get; }

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

    FullFmuLibraryPath = $"{Path.GetFullPath(extractedFolderPath + osDependentPath + "/" + ModelDescription.ModelName)}";
    InitializeModelBinding(FullFmuLibraryPath);
  }

  private void InitializeModelDescription(string extractedFolderPath)
  {
    ModelDescription = ModelLoader.LoadModelFromExtractedPath(extractedFolderPath);
  }

  private void InitializeModelBinding(string fullPathToLibrary)
  {
    DllPtr = LoadFmiLibrary(fullPathToLibrary);
    InitializeFmiDelegates();
  }


  private IntPtr LoadFmiLibrary(string libraryPath)
  {
    var res = NativeMethods.LoadLibrary(libraryPath);
    if (res == IntPtr.Zero)
    {
      throw new FileLoadException("Failed to retrieve a pointer from the provided FMU library");
    }

    return res;
  }

  protected abstract void InitializeFmiDelegates();

  protected void SetDelegate<T>(out T deleg) where T : System.Delegate
  {
    var delegateTypeName = typeof(T).Name;
    if (string.IsNullOrEmpty(delegateTypeName))
    {
      throw new FileLoadException($"Failed to retrieve method name by reflection.");
    }
    delegateTypeName = delegateTypeName.Substring(0, delegateTypeName.Length - 4);

    var ptr = NativeMethods.GetProcAddress(DllPtr, delegateTypeName);
    if (ptr == IntPtr.Zero)
    {
      throw new FileLoadException(
        $"Failed to retrieve function pointer to method '{delegateTypeName}'.");
    }
      
    deleg = (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
  }

  public abstract void DoStep(
    double currentCommunicationPoint,
    double communicationStepSize,
    out double lastSuccessfulTime);

  public abstract void Terminate();


  ~FmiBindingBase()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool mDisposedValue;
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
}
