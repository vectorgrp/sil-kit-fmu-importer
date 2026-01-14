// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services;
using SilKit.Services.Logger;
using SilKit.Services.Rpc;
using System.Runtime.InteropServices;

namespace FmuImporter.SilKit;

public class SilKitRpcServerManager : SilKitRpcManager
{
  public Dictionary<uint /* vRef Rx_CallId*/, IRpcServer> Servers { get; }
  public Dictionary<uint /* vRef Rx_CallId */, Dictionary<ulong /* Rx_CallId */, IntPtr /* callHandle */>> CallIdHandles { get; }

  // default ctor if no RPC to manage
  public SilKitRpcServerManager() : base()
  {
    Servers = new Dictionary<uint, IRpcServer>();
    CallIdHandles = new Dictionary<uint, Dictionary<ulong, IntPtr>>();
  }

  public SilKitRpcServerManager(SilKitEntity silKitEntity) : base(silKitEntity)
  {
    Servers = new Dictionary<uint, IRpcServer>();
    CallIdHandles = new Dictionary<uint, Dictionary<ulong, IntPtr>>();
  }

  #region service creation
  public bool CreateRpcServer(string controllerName, RpcSpec dataSpec, IntPtr /* vRef Rx_CallId */ context, RpcCallHandler callHandler)
  {
    var server = _silKitEntity.CreateRpcServer(controllerName, dataSpec, context, callHandler);
    return Servers.TryAdd((uint)context, server);
  }
  #endregion service creation

  #region data collection & processing
  public void SubmitResult(List<Tuple<uint /* vRef Tx_Id */, Tuple<ulong /* Tx_Id */, byte[]?>>> operationList)
  {
    foreach (var refData in operationList)
    {
      var vRefTx = refData.Item1;
      var returnIdArgs = refData.Item2;

      if (!TxToRxMapping.TryGetValue(vRefTx, out var vRefRx))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"No Rx vRef mapping found for Tx vRef {vRefTx}");
        return;
      }

      if (!Servers.TryGetValue(vRefRx, out var server))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"Trying to submit RPC result: no RPC server found for value " +
          $"reference {vRefRx}.");
        continue;
      }

      if (!CallIdHandles.TryGetValue(vRefRx, out var idCallHandle))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"No call id and handle found for value reference {vRefRx}");
        continue;
      }

      if (!idCallHandle.TryGetValue(returnIdArgs.Item1, out var callHandle))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"No call handle found for value reference {vRefRx}");
        continue;
      }

      var vBytes = new ByteVector();
      try
      {
        if (returnIdArgs.Item2 is not null)
        {
          int size = returnIdArgs.Item2.Length;
          IntPtr ptr = Marshal.AllocHGlobal(size);
          Marshal.Copy(returnIdArgs.Item2, 0, ptr, size);

          vBytes.data = ptr;
          vBytes.size = size;
        }

        server.SubmitResult(callHandle, vBytes);
        // clean up the call handle after successful submission
        idCallHandle.Remove(returnIdArgs.Item1);
      }
      catch (Exception ex)
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"Failed to submit RPC result for vRef {vRefRx}: {ex.Message}");
      }
      finally
      {
        Marshal.FreeHGlobal(vBytes.data);
      }
    }
  }

  private ulong _internalIds = 0;
  public void FuncRpcCallHandler(IntPtr context, IntPtr server, IntPtr callEvent)
  {
    try
    {
      uint vRefRx = (uint)context.ToInt32();

      var cEvent = Marshal.PtrToStructure<RpcCallEvent>(callEvent);

      if (CallIdHandles.TryGetValue(vRefRx, out var idHandle))
      {
        idHandle.TryAdd(_internalIds, cEvent.callHandle);
      }
      else
      {
        var newDict = new Dictionary<ulong, IntPtr>();
        newDict.TryAdd(_internalIds, cEvent.callHandle);
        CallIdHandles.Add(vRefRx, newDict);
      }

      var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0L : cEvent.timestampInNs;

      AddToEventBuffer(timeStamp, vRefRx, _internalIds, cEvent.argumentData);
      
      ++_internalIds;
    }
    catch (Exception e)
    {
      _silKitEntity.Logger.Log(LogLevel.Error, $"Error processing RPC call: {e.Message}");
      _silKitEntity.Logger.Log(LogLevel.Debug, $"Exception details: {e}");
    }
  }

  public Dictionary<uint, List<Tuple<ulong, byte[]?>>> RetrieveReceivedRpcCalls(ulong currentTime)
  {
    return RetrieveEvents(currentTime);
  }
  #endregion data collection & processing
}
