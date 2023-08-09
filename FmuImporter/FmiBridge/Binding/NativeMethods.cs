// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace Fmi.Binding;

internal static class NativeMethods
{
  public delegate IntPtr DlSymbol(IntPtr hModule, string procedureName);

  public static DlSymbol? DlSymbolDelegate;

  public delegate int DlClose(IntPtr hModule);

  public static DlClose? DlCloseDelegate;


  [DllImport("kernel32.dll")]
  public static extern IntPtr LoadLibrary(string dllToLoad);

  [DllImport("kernel32.dll")]
  public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

  [DllImport("kernel32.dll")]
  public static extern bool FreeLibrary(IntPtr hModule);

  public static IntPtr dlopen(string dllToLoad, int flags)
  {
    IntPtr ptr;
    try
    {
      ptr = NativeMethodsLinuxOld.dlopen(dllToLoad, flags);
      DlSymbolDelegate = NativeMethodsLinuxOld.dlsym;
      DlCloseDelegate = NativeMethodsLinuxOld.dlclose;
      return ptr;
    }
    catch (DllNotFoundException)
    {
      ptr = NativeMethodsLinuxNew.dlopen(dllToLoad, flags);
      DlSymbolDelegate = NativeMethodsLinuxNew.dlsym;
      DlCloseDelegate = NativeMethodsLinuxNew.dlclose;
      return ptr;
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }
  }

  public static IntPtr dlsym(IntPtr hModule, string procedureName)
  {
    if (DlSymbolDelegate == null)
    {
      throw new Exception("Initialization error -> dlopen must be called before dlsym");
    }

    return DlSymbolDelegate(hModule, procedureName);
  }

  public static int dlclose(IntPtr hModule)
  {
    if (DlCloseDelegate == null)
    {
      throw new Exception("Initialization error -> dlopen must be called before dlsym");
    }

    return DlCloseDelegate(hModule);
  }
}

/// <summary>
///   NB: The following classes work around an issue occurring in Ubuntu 22.04 and up.
///   These distributions do not have a libdl.so anymore and they use libdl.so.2 instead.
///   To prevent issues, libdl.so.2 is loaded first and if this fails, libdl.so is loaded.
/// </summary>
internal class NativeMethodsLinuxNew
{
  [DllImport("c")]
  public static extern IntPtr dlopen(string dllToLoad, int flags);

  [DllImport("c")]
  public static extern IntPtr dlsym(IntPtr hModule, string procedureName);

  [DllImport("c")]
  public static extern int dlclose(IntPtr hModule);
}

internal class NativeMethodsLinuxOld
{
  [DllImport("dl")]
  public static extern IntPtr dlopen(string dllToLoad, int flags);

  [DllImport("dl")]
  public static extern IntPtr dlsym(IntPtr hModule, string procedureName);

  [DllImport("dl")]
  public static extern int dlclose(IntPtr hModule);
}
