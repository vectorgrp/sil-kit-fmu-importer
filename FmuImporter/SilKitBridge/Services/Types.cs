// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services
{
  public enum LabelKind : Int32
  {
    //! Undefined.
    Undefined = 0,
    //! If this label is available, its value must match.
    Optional = 1,
    //! This label must be available and its value must match.
    Mandatory = 2
  }

  [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
  public struct LabelList
  {
    public IntPtr /* size_t */ numLabels;
    public IntPtr labels;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
  public struct Label
  {
    [MarshalAs(UnmanagedType.LPStr)] public string key;
    [MarshalAs(UnmanagedType.LPStr)] public string value;
    public int kind;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  public struct ByteVector
  {
    public IntPtr data;
    public IntPtr /*size_t*/ size;
  }
}
