// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using static SilKit.SilKitVersion;

namespace SilKit.Services.Rpc;

public static class RpcTypes
{
  public static string MediaTypeRpc()
  {
    return "application/vnd.vector.silkit.rpc; protocolVersion=1";
  }
}

public enum RpcCallStatus : Int32
{
  //! Call was successful.
  Success = 0,
  //! No server matching the RpcSpec was found.
  ServerNotReachable = 1,
  //! An unidentified error occured.
  UndefinedError = 2,
  // The Call lead to an internal RpcServer error.
  // This might happen if no CallHandler was specified for the RpcServer.
  InternalServerError = 3,
  // The Call did run into a timeout and was canceled.
  // This might happen if a corresponding server crashed, ran into an error or took too long to answer the call
  Timeout = 4
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct RpcCallResultEvent
{
  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public IntPtr userContext;
  public RpcCallStatus status;
  public ByteVector resultData;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct RpcCallEvent
{
  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public IntPtr callHandle;
  public ByteVector argumentData;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct RpcSpec
{
  public RpcSpec()
  {
    structHeader = GetStructHeader(ServiceId.Rpc, DatatypeId.RpcSpec);
    functionName = "";
    mediaType = "";
    labelList = new LabelList();
  }

  internal StructHeader structHeader;
  public string functionName;
  public string mediaType;
  public LabelList labelList;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RpcCallResultHandler(
  IntPtr context,
  IntPtr client,
  IntPtr resultEvent);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RpcCallHandler(
  IntPtr context,
  IntPtr server,
  IntPtr callEvent);
