// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services;
using SilKit.Services.Logger;
using SilKit.Services.Rpc;
using System.Runtime.InteropServices;

namespace FmuImporter.SilKit;

public class SilKitRpcClientManager : SilKitRpcManager
{
  public Dictionary<uint /* vRef Rx_ReturnId */, IRpcClient> Clients { get; }

  // default ctor if no RPC to manage
  public SilKitRpcClientManager() : base()
  {
    Clients = new Dictionary<uint, IRpcClient>();
  }

  public SilKitRpcClientManager(SilKitEntity silKitEntity) : base(silKitEntity)
  {
    Clients = new Dictionary<uint, IRpcClient>();
  }

  #region service creation
  public bool CreateRpcClient(string controllerName, RpcSpec dataSpec, IntPtr /* vRef Rx_ReturnId */ resultHandlerContext, RpcCallResultHandler handler)
  {
    var client = _silKitEntity.CreateRpcClient(controllerName, dataSpec, resultHandlerContext, handler);
    return Clients.TryAdd((uint)resultHandlerContext, client);
  }
  #endregion service creation

  #region data collection & processing
  public void Call(List<Tuple<uint, Tuple<ulong, byte[]?>>> operationList)
  {
    foreach (var refData in operationList)
    {
      var vRefTx = refData.Item1;
      var callIdArgs = refData.Item2;

      if (!TxToRxMapping.TryGetValue(vRefTx, out var vRefRx))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"No Tx vRef mapping found for Rx vRef {vRefTx}");
        return;
      }

      if (!Clients.TryGetValue(vRefRx, out var client))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"Trying to make a RPC call: no RPC client found for value reference {vRefRx}");
        continue;
      }

      var vBytes = new ByteVector();
      try
      {
        if (callIdArgs.Item2 is not null)
        {
          var size = callIdArgs.Item2.Length;
          IntPtr ptr = Marshal.AllocHGlobal(size);
          Marshal.Copy(callIdArgs.Item2.ToArray(), 0, ptr, size);
          vBytes.data = ptr;
          vBytes.size = (IntPtr)size;
        }
#if X86
        var userContext = callIdArgs.Item1;
        if (userContext > uint.MaxValue)
        {
          _silKitEntity.Logger.Log(LogLevel.Error, $"RPC call failed for vRef {vRefTx}: call ID {userContext} exceeds 32-bit limit on x86 platform");
          continue;
        }
        client.Call(vBytes, (IntPtr)(uint)userContext);
#else
        client.Call(vBytes, (IntPtr)callIdArgs.Item1);
#endif
      }
      catch (Exception ex)
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"Failed to make RPC call for vRef {vRefRx}: {ex.Message}");
      }
      finally
      {
        Marshal.FreeHGlobal(vBytes.data);
      }
    }
  }

  public void FuncRpcCallResultHandler(IntPtr context, IntPtr client, IntPtr resultEvent)
  {
    try
    { 
      var vRef = (uint)context; // passed when creating the client

      var callResultEvent = Marshal.PtrToStructure<RpcCallResultEvent>(resultEvent);

      var returnId = (ulong)callResultEvent.userContext;

      if (callResultEvent.status != RpcCallStatus.Success)
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"Error while receiving CallResult - Status: {callResultEvent.status}");
        return;
      }

      var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0L : callResultEvent.timestampInNs;
      
      AddToEventBuffer(timeStamp, vRef, returnId, callResultEvent.resultData);
    }
    catch (Exception ex)
    {
      _silKitEntity.Logger.Log(LogLevel.Error, $"Error processing RPC result: {ex.Message}");
      _silKitEntity.Logger.Log(LogLevel.Debug, $"Exception details: {ex}");
    }
  }

  public Dictionary<uint, List<Tuple<ulong, byte[]?>>> RetrieveReceivedRpcEvents(ulong currentTime)
  {
    return RetrieveEvents(currentTime);
  }
  #endregion data collection & processing
}
