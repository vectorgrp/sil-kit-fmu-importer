// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services;
using System.Runtime.InteropServices;

namespace FmuImporter.SilKit;

public class SilKitRpcManager
{
  protected readonly SilKitEntity _silKitEntity;
  public SortedList<ulong /* timestamp */, Dictionary<uint /* vRef Rx_Id*/, Dictionary<ulong /* Rx_Id */, byte[]?>>> EventBuffer { get; }
  public Dictionary<uint /* vRef Tx_Id */, uint /* vRef Rx_Id */> TxToRxMapping { get; }

  public void AddTxRxMapping(uint txVRef, uint rxVRef)
  {
    TxToRxMapping[txVRef] = rxVRef;
  }

  // default ctor if no RPC to manage
  public SilKitRpcManager()
  {
    _silKitEntity = null!;
    EventBuffer = new SortedList<ulong, Dictionary<uint, Dictionary<ulong, byte[]?>>>();
    TxToRxMapping = new Dictionary<uint, uint>();
  }

  public SilKitRpcManager(SilKitEntity silKitEntity) : this()
  {
    _silKitEntity = silKitEntity;

    if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      EventBuffer.Add(0, new Dictionary<uint, Dictionary<ulong, byte[]?>>());
    }
  }

  #region data collection & processing
  public void AddToEventBuffer(ulong timeStamp, uint vRef, ulong id, ByteVector vBytes)
  {
    if (vBytes.size == IntPtr.Zero)
    {
      AddToEventBuffer(timeStamp, vRef, id, null);
    }
    else
    {
      var dataSize = vBytes.size.ToInt32();
      byte[] bytes = new byte[dataSize];
      Marshal.Copy(vBytes.data, bytes, 0, dataSize);

      AddToEventBuffer(timeStamp, vRef, id, bytes);
    }
  }

  public void AddToEventBuffer(ulong timeStamp, uint vRef, ulong id, byte[]? data)
  {
    if (EventBuffer.TryGetValue(timeStamp, out var refDict))
    {
      if (refDict.TryGetValue(vRef, out var futureDict))
      {
        futureDict[id] = data;
      }
      else
      {
        var dict = new Dictionary<ulong, byte[]?> { { id, data } };
        refDict[vRef] = dict;
      }
    }
    else
    {
      var dict = new Dictionary<uint, Dictionary<ulong, byte[]?>>
      {
        { vRef, new Dictionary<ulong, byte[]?> { { id, data } } }
      };
      EventBuffer.Add(timeStamp, dict);
    }
  }

  public Dictionary<uint, List<Tuple<ulong, byte[]?>>> RetrieveEvents(ulong currentTime)
  {
    var removeCounter = 0;
    var valueUpdates = new Dictionary<uint, List<Tuple<ulong, byte[]?>>>();

    foreach (var (timeStamp, rpcEvent) in EventBuffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeStamp <= currentTime)
      {
        foreach (var vRefRpcEvent in rpcEvent)
        {
          valueUpdates[vRefRpcEvent.Key] = new List<Tuple<ulong, byte[]?>>();
          foreach (var idDataPair in vRefRpcEvent.Value)
          {
            valueUpdates[vRefRpcEvent.Key].Add(new Tuple<ulong, byte[]?>(idDataPair.Key, idDataPair.Value));
          }
        }
        removeCounter++;
      }
      else
      {
        break;
      }
    }

    // Remove all processed entries from the buffer
    while (removeCounter-- > 0)
    {
      EventBuffer.RemoveAt(0);
    }

    return valueUpdates;
  }
  #endregion data collection & processing
}
