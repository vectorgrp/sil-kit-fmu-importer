// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;
using SilKit.Services.Ethernet;
using SilKit.Services.Logger;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace FmuImporter.SilKit;

public class SilKitEthernetManager
{
  private readonly SilKitEntity _silKitEntity;
  private readonly DataConverter _dc;
  public Dictionary<uint /* vRefOut Tx_Data*/, IEthernetController> EthControllers { get; }
  public SortedList<ulong /* timestamp */, Dictionary<uint /* vRef */, List<byte[]>>> EthBuffer { get; }

  // default ctor if no Ethernet traffic to manage
  public SilKitEthernetManager()
  {
    _silKitEntity = null!;
    _dc = null!;
    EthControllers = new Dictionary<uint, IEthernetController>();
    EthBuffer = new SortedList<ulong, Dictionary<uint, List<byte[]>>>();
  }

  public SilKitEthernetManager(SilKitEntity silKitEntity)
  {
    _silKitEntity = silKitEntity;
    _dc = new DataConverter();

    EthBuffer = new SortedList<ulong, Dictionary<uint, List<byte[]>>>();
    if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      EthBuffer.Add(0, new Dictionary<uint, List<byte[]>>());
    }

    EthControllers = new Dictionary<uint, IEthernetController>();
  }

  #region service creation
  public bool CreateEthernetController(string controllerName, string networkName, uint vRefOut)
  {
    var ethernetController = _silKitEntity.CreateEthernetController(controllerName, networkName);
    return EthControllers.TryAdd(vRefOut, ethernetController);
  }

  public void ActivateEthernetControllers()
  {
    foreach (var ethernetController in EthControllers.Values)
    {
      ethernetController.Activate();
    }
  }

  public void DeactivateEthernetControllers()
  {
    foreach (var ethernetController in EthControllers.Values)
    {
      ethernetController.Deactivate();
    }
  }

  public UInt64 AddEthernetFrameHandler(uint vRef, uint vRefIn, EthernetFrameHandler handler, byte directionMask)
  {
    EthControllers.TryGetValue(vRef, out var ethernetController);
    if (ethernetController == null)
    {
      throw new NullReferenceException($"No Ethernet controller found for value reference {vRef}");
    }
    return ethernetController.AddFrameHandler((IntPtr)vRefIn, handler, directionMask);
  }

  public UInt64 AddFrameTransmitHandler(uint vRef, EthernetFrameTransmitHandler handler, UInt32 statusMask)
  {
    EthControllers.TryGetValue(vRef, out var ethernetController);
    if (ethernetController == null)
    {
      throw new NullReferenceException($"No Ethernet controller found for value reference {vRef}");
    }
    return ethernetController.AddFrameTransmitHandler((IntPtr)vRef, handler, statusMask);
  }

  public void SendAllFrames(List<Tuple<uint, byte[]>> ethFrames)
  {
    foreach (var pairRefOperation in ethFrames)
    {
      // Check the OP Code : 4 first bytes
      var operation = (EthernetOperations)BinaryPrimitives.ReadUInt32LittleEndian(pairRefOperation.Item2);
      switch (operation)
      {
        case EthernetOperations.Format_Error:
          {
            // log the whole operation that caused the error. Data starts after 29 bytes of fixed header
            _silKitEntity.Logger.Log(LogLevel.Warn, $"Format Error Operation received on Tx_Data with value " +
              $"reference {pairRefOperation.Item1}. Complete binary data that caused the error: " +
              $"{pairRefOperation.Item2.Skip(28).ToArray()}");
            break;
          }
        case EthernetOperations.Transmit:
          {
            SendFrame(pairRefOperation.Item1, pairRefOperation.Item2);
            break;
          }
        case EthernetOperations.Confirm:
        case EthernetOperations.Bus_Error:
        case EthernetOperations.Configuration:
        case EthernetOperations.Wakeup:
          {
            // unsupported operation
            _silKitEntity.Logger.Log(LogLevel.Warn, $"Unsupported {operation} Operation received on Tx_Data with value " +
              $"reference {pairRefOperation.Item1}");
            break;
          }
        default:
          {
            // non existing operation
            _silKitEntity.Logger.Log(LogLevel.Warn, $"Non existing Operation received on Tx_Data with value " +
              $"reference {pairRefOperation.Item1}. Operation code received is: {operation}");
            break;
          }
      }
    }
  }

  public void SendFrame(uint vRef, byte[] data)
  {
    EthControllers.TryGetValue(vRef, out var ethController);
    if (ethController == null)
    {
      _silKitEntity.Logger.Log(LogLevel.Error, $"Trying to send an Ethernet frame: no Ethernet controller found for value " +
        $"reference {vRef}");
      return;
    }
    ethController.SendFrame(_dc.LsEthernetTransmitOperationToSilKitEthernetFrame(data, _silKitEntity.Logger), (IntPtr)ethController.TransmitId);
    ethController.TransmitId++;
  }
  #endregion service creation

  #region data collection & processing
  public void FuncEthernetFrameHandler(IntPtr context, IntPtr controller, IntPtr frameEvent)
  {
    // Use a last-is-best approach for storage
    var valueRef = (uint)context;
    var ethFrameEvent = Marshal.PtrToStructure<EthernetFrameEvent>(frameEvent);
    var ethFrame = Marshal.PtrToStructure<EthernetFrame>(ethFrameEvent.ethernetFrame);
    var bytes = _dc.SilKitEthernetFrameToLsEthernetTransmitOperation(ethFrame);

    var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0L : ethFrameEvent.timestampInNs;

    // data is processed in sim. step callback (OnSimulationStep)
    if (EthBuffer.TryGetValue(timeStamp, out var futureDict))
    {
      futureDict[valueRef].Add(bytes);
    }
    else
    {
      var dict = new Dictionary<uint, List<byte[]>>
      {
        { valueRef, new List<byte[]> { bytes } }
      };
      EthBuffer.Add(timeStamp, dict);
    }
  }

  public void FuncEthernetFrameTransmitHandler(IntPtr context, IntPtr controller, IntPtr frameTransmitEvent)
  {
    var ethFrameTransmitEvent = Marshal.PtrToStructure<EthernetFrameTransmitEvent>(frameTransmitEvent);

    _silKitEntity.Logger.Log(LogLevel.Debug, (ethFrameTransmitEvent.status == EthernetTransmitStatus.Transmitted ? "ACK" : "NACK") +
      " for ethernet frame with transmitId=" + (int)ethFrameTransmitEvent.userContext +
      ", timestamp: " + ethFrameTransmitEvent.timestampInNs);
  }

  public Dictionary<uint, List<byte[]>> RetrieveReceivedEthernetData(ulong currentTime)
  {
    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var removeCounter = 0;
    var valueUpdates = new Dictionary<uint, List<byte[]>>();
    foreach (var (timeStamp, ethData) in EthBuffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeStamp <= currentTime)
      {
        foreach (var refFramePair in ethData)
        {
          valueUpdates[refFramePair.Key] = new List<byte[]>();

          foreach (var frame in refFramePair.Value)
          {
            valueUpdates[refFramePair.Key].Add(frame);
          }
        }
        removeCounter++;
      }
      else
      {
        // no need to iterate future events
        break;
      }
    }

    // remove all processed entries from the buffer
    while (removeCounter-- > 0)
    {
      EthBuffer.RemoveAt(0);
    }

    return valueUpdates;
  }
  #endregion data collection & processing

}
