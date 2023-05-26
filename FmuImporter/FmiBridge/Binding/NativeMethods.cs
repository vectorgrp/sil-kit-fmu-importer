// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace Fmi.Binding;

internal static class NativeMethods
{
#if OS_WINDOWS
  [DllImport("kernel32.dll")]
  public static extern IntPtr LoadLibrary(string dllToLoad);

  [DllImport("kernel32.dll")]
  public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

  [DllImport("kernel32.dll")]
  public static extern bool FreeLibrary(IntPtr hModule);
#elif (OS_LINUX || OS_MAC)
    [DllImport("libdl")]
    public static extern IntPtr dlopen(string dllToLoad, int flags);

    [DllImport("libdl")]
    public static extern IntPtr dlsym(IntPtr hModule, string procedureName);

    [DllImport("libdl")]
    public static extern int dlclose(IntPtr hModule);
#endif
}
