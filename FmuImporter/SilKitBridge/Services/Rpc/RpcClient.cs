// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.Rpc;

internal class RpcClient : IRpcClient
{
  private readonly Participant _participant;
  private readonly RpcCallResultHandler _handler;
  private IntPtr _rpcClientPtr;

  internal IntPtr RpcClientPtr
  {
    get
    {
      return _rpcClientPtr;
    }
    private set
    {
      _rpcClientPtr = value;
    }
  }

  internal RpcClient(Participant participant, string controllerName, RpcSpec dataSpec, IntPtr resultHandlerContext,
    RpcCallResultHandler handler)
  {
    _participant = participant;
    _handler = handler;

    var dataSpecPtr = Marshal.AllocHGlobal(Marshal.SizeOf<RpcSpec>());
    Marshal.StructureToPtr(dataSpec, dataSpecPtr, false);
    try
    {
      Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_RpcClient_Create(
        out _rpcClientPtr,
        _participant.ParticipantPtr,
        controllerName,
        dataSpecPtr,
        resultHandlerContext,
        _handler),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    finally
    {
      Marshal.FreeHGlobal(dataSpecPtr);
    }
  }

  /*
      SilKit_RpcClient_Create(
          SilKit_RpcClient** outClient,
          SilKit_Participant* participant,
          const char* controllerName, SilKit_RpcSpec* rpcSpec,
          void* resultHandlerContext,
          SilKit_RpcCallResultHandler_t resultHandler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_RpcClient_Create(
    [Out] out IntPtr outClient,
    [In] IntPtr participant,
    [In, MarshalAs(UnmanagedType.LPStr)] string cName,
    [In] IntPtr rpcSpec,
    [In] IntPtr resultHandlerContext,
    [In] RpcCallResultHandler resultHandler);

  public void Call(ByteVector data, IntPtr userContext)
  {
    var bytesVectorPtr = Marshal.AllocHGlobal(Marshal.SizeOf<ByteVector>());
    Marshal.StructureToPtr(data, bytesVectorPtr, false);
    try
    {
      Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_RpcClient_Call(
        _rpcClientPtr,
        bytesVectorPtr,
        userContext),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    finally
    {
      Marshal.FreeHGlobal(bytesVectorPtr);
    }
  }

  /*
      SilKit_RpcClient_Call(
          SilKit_RpcClient* self, 
          const SilKit_ByteVector* argumentData,
          void* userContext);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_RpcClient_Call(
    [In] IntPtr self,
    [In] IntPtr argumentData,
    [In] IntPtr userContext);
}