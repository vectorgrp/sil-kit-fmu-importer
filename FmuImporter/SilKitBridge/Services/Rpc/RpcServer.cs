// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.Rpc;

public class RpcServer : IRpcServer
{
  private readonly Participant _participant;
  private readonly RpcCallHandler _callHandler;
  private IntPtr _rpcServerPtr;

  public IntPtr RpcServerPtr
  {
    get
    {
      return _rpcServerPtr;
    }
    private set
    {
      _rpcServerPtr = value;
    }
  }

  internal RpcServer(Participant participant, string controllerName, RpcSpec dataSpec, IntPtr context,
    RpcCallHandler callHandler)
  {
    _participant = participant;
    _callHandler = callHandler;

    var dataSpecPtr = Marshal.AllocHGlobal(Marshal.SizeOf<RpcSpec>());
    Marshal.StructureToPtr(dataSpec, dataSpecPtr, false);
    try
    {
      Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_RpcServer_Create(
        out _rpcServerPtr,
        _participant.ParticipantPtr,
        controllerName,
        dataSpecPtr,
        context,
        _callHandler),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    finally
    {
      Marshal.FreeHGlobal(dataSpecPtr);
    }
  }

  /*
      SilKit_RpcServer_Create(
          SilKit_RpcServer** out,
          SilKit_Participant* participant,
          const char* controllerName, SilKit_RpcSpec* rpcSpec,
          void* context,
          SilKit_RpcCallHandler_t callHandler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_RpcServer_Create(
    [Out] out IntPtr outServer,
    [In] IntPtr participant,
    [In, MarshalAs(UnmanagedType.LPStr)] string cName,
    [In] IntPtr rpcSpec,
    [In] IntPtr context,
    [In] RpcCallHandler callHandler);

  public void SubmitResult(IntPtr callHandle, ByteVector resultData)
  {
    var byteVectorPtr = Marshal.AllocHGlobal(Marshal.SizeOf<ByteVector>());
    Marshal.StructureToPtr(resultData, byteVectorPtr, false);
    try
    {
      Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_RpcServer_SubmitResult(
        RpcServerPtr,
        callHandle,  // Pass the callHandle directly as IntPtr
        byteVectorPtr),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    finally
    {
      Marshal.FreeHGlobal(byteVectorPtr);
    }
  }

  /*
      SilKit_RpcServer_SubmitResult(
          SilKit_RpcServer* self,
          SilKit_RpcCallHandle* callHandle,
          const SilKit_ByteVector* returnData);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_RpcServer_SubmitResult(
    [In] IntPtr self,
    [In] IntPtr callHandle,
    [In] IntPtr returnData);
}