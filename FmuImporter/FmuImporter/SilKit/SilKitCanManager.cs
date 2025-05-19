// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;
using SilKit.Services.Can;
using SilKit.Services.Logger;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace FmuImporter.SilKit;

public class SilKitCanManager
{
  private readonly SilKitEntity _silKitEntity;
  private readonly DataConverter _dc;
  public Dictionary<uint /* vRefOut Tx_Data*/, ICanController> CanControllers { get; }
  public SortedList<ulong /* timestamp */, Dictionary<uint /* vRef */, Dictionary<uint /* CAN id */, byte[]>>> CanBuffer { get; }

  // default ctor if no CAN traffic to manage
  public SilKitCanManager()
  {
    _silKitEntity = null!;
    _dc = null!;
    CanControllers = new Dictionary<uint , ICanController>();
    CanBuffer = new SortedList<ulong, Dictionary<uint, Dictionary<uint, byte[]>>>();
  }

  public SilKitCanManager(SilKitEntity silKitEntity)
  {
    _silKitEntity = silKitEntity;
    _dc = new DataConverter();

    CanBuffer = new SortedList<ulong, Dictionary<uint, Dictionary<uint, byte[]>>>();
    if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      CanBuffer.Add(0, new Dictionary<uint, Dictionary<uint, byte[]>>());
    }

    CanControllers = new Dictionary<uint, ICanController>();
  }

#region service creation
  public bool CreateCanController(string controllerName, string networkName, uint vRefOut)
  {
    var canController = _silKitEntity.CreateCanController(controllerName, networkName);
    return CanControllers.TryAdd(vRefOut, canController);
  }

  public void StartCanControllers()
  {
    foreach (var canController in CanControllers.Values)
    {
      canController.Start();
    }
  }

  public void StopCanControllers()
  {
    foreach (var canController in CanControllers.Values)
    {
      canController.Stop();
    }
  }

  public UInt64 AddCanFrameHandler(uint vRef, long vRefIn, CanFrameHandler handler, byte directionMask)
  {
    CanControllers.TryGetValue(vRef, out var canController);
    if (canController == null)
    {
      throw new NullReferenceException($"No CAN controller found for value reference {vRef}");
    }
    return canController.AddFrameHandler((IntPtr)vRefIn, handler, directionMask);
  }

  public UInt64 AddFrameTransmitHandler(uint vRef, CanFrameTransmitHandler handler, Int32 statusMask)
  {
    CanControllers.TryGetValue(vRef, out var canController);
    if (canController == null)
    {
      throw new NullReferenceException($"No CAN controller found for value reference {vRef}");
    }
    return canController.AddFrameTransmitHandler((IntPtr)vRef, handler, statusMask);
  }

  public void SendAllFrames(List<Tuple<uint, byte[]>> canList)
  {
    foreach (var pairRefOperation in canList)
    {
      // Check the OP Code : 4 first bytes
      var operation = (CanOperations)BinaryPrimitives.ReadUInt32LittleEndian(pairRefOperation.Item2);
      switch (operation)
      {
        case CanOperations.Format_Error:
        {
          // log the whole operation that caused the error. Data starts at the 11th byte
          _silKitEntity.Logger.Log(LogLevel.Warn, $"Format Error Operation received on Tx_Data with value " +
            $"reference {pairRefOperation.Item1}. Complete binary data that caused the error: " +
            $"{pairRefOperation.Item2.Skip(10).ToArray()}");
          break;
        }
        case CanOperations.CAN_Transmit:
        {
          SendFrame(pairRefOperation.Item1, pairRefOperation.Item2);
          break;
        }
        case CanOperations.CAN_FD_Transmit:
        case CanOperations.CAN_XL_Transmit:
        case CanOperations.Confirm:
        case CanOperations.Arbitration_Lost:
        case CanOperations.Bus_Error:
        case CanOperations.Configuration:
        case CanOperations.Status:
        case CanOperations.Wakeup:
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
    if (data.Length < 16)
    {
      // can transmit operation malformed
      _silKitEntity.Logger.Log(LogLevel.Warn, $"The retrieved CAN operation is malformed. Bytes retrieved: {data}");
    }

    CanControllers.TryGetValue(vRef, out var canController);
    if ( canController == null )
    {
      _silKitEntity.Logger.Log(LogLevel.Error, $"Trying to send a CAN frame: no CAN controller found for value " +
        $"reference {vRef}");
      return;
    }
    canController.SendFrame(_dc.LsCanTransmitOperationToSilKitCanFrame(data, _silKitEntity.Logger), (IntPtr)canController.transmitId);
    canController.transmitId++;
  }
#endregion service creation

#region data collection & processing
  public void FuncCanFrameHandler(IntPtr context, IntPtr controller, IntPtr frameEvent)
  {
    // Use a last-is-best approach to store CAN frames with their CAN id
    var valueRef = (uint)context;
    var cFrameEvent = Marshal.PtrToStructure<CanFrameEvent>(frameEvent);
    var canFrame = Marshal.PtrToStructure<CanFrame>(cFrameEvent.frame);
    var bytes = _dc.SilKitCanFrameToLsCanTransmitOperation(canFrame);

    var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0L : cFrameEvent.timestampInNs;

    // data is processed in sim. step callback (OnSimulationStep)
    if (CanBuffer.TryGetValue(timeStamp, out var refDict))
    {
      if (refDict.TryGetValue(valueRef, out var futureDict))
      {
        futureDict[canFrame.id] = bytes; // add or update can frame with this CAN id
      }
      else
      {
        var dict = new Dictionary<uint, byte[]>
        {
          { canFrame.id, bytes }
        };
        refDict[valueRef] = dict;
      }
    }
    else
    {
      var dict = new Dictionary<uint, Dictionary<uint, byte[]>>
      {
        { valueRef, new Dictionary<uint, byte[]> { { canFrame.id, bytes } } }
      };
      CanBuffer.Add(timeStamp, dict);
    }
  }

  public void FuncCanFrameTransmitHandler(IntPtr context, IntPtr controller, IntPtr frameTransmitEvent)
  {
    var canFrameTransmitEvent = Marshal.PtrToStructure<CanFrameTransmitEvent>(frameTransmitEvent);

    _silKitEntity.Logger.Log(LogLevel.Debug, (canFrameTransmitEvent.status == CanTransmitStatus.Transmitted ? "ACK" : "NACK") +
      " for CAN Message with transmitId=" + (int)canFrameTransmitEvent.userContext + 
      ", can ID: " + canFrameTransmitEvent.canId.ToString("X") + 
      ", timestamp: " + canFrameTransmitEvent.timestampInNs);
  }

  public Dictionary<uint, List<byte[]>> RetrieveReceivedCanData(ulong currentTime)
  {
    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var removeCounter = 0;
    var valueUpdates = new Dictionary<uint, List<byte[]>>();
    foreach (var (timeStamp, canData) in CanBuffer)
    {
      if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeStamp <= currentTime)
      {
        foreach (var refFramePair in canData)
        {
          valueUpdates[refFramePair.Key] = new List<byte[]>();

          foreach (var idDataPair in refFramePair.Value)
          {
            valueUpdates[refFramePair.Key].Add(idDataPair.Value);
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
      CanBuffer.RemoveAt(0);
    }

    return valueUpdates;
  }
#endregion data collection & processing

}
