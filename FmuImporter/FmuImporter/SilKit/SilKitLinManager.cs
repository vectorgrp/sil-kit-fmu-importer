// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;
using SilKit.Services.Lin;
using SilKit.Services.Logger;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace FmuImporter.SilKit;

public class SilKitLinManager
{
  private readonly SilKitEntity _silKitEntity;
  private readonly object _linControllersLock = new();
  private readonly object _linBufferLock = new();
  private const byte silKitUnknownDataLength = 255; // SILKIT_LIN_UNKNOWN_DATA_LENGTH
  private Dictionary<uint /* vRefOut Tx_Data */, LinControllerContext> LinControllers { get; }

  // buffered received LIN frame responses, keyed by timestamp -> vRefIn (Rx_Data) -> LIN id -> operation bytes
  private SortedList<ulong /* timestamp */, Dictionary<uint /* vRefIn */, Dictionary<byte /* LIN id */, byte[]>>> LinBuffer { get; }

  private sealed class LinControllerContext
  {
    public ILinController Controller { get; }

    // LIN IDs that have already been configured with SilKit_LinController_SetFrameResponse
    // as Rx, so the controller actually surfaces LIN_RX_OK events for responses coming
    // from other (Responder/Slave) participants.
    public HashSet<byte> ConfiguredRxIds { get; }

    public LinControllerContext(ILinController controller)
    {
      Controller = controller;
      ConfiguredRxIds = new HashSet<byte>();
    }
  }

  private enum LinOperations : uint
  {
    Format_Error = 0x01,
    Transmit = 0x10,
    Confirm = 0x20,
    Bus_Error = 0x30,
    Configuration = 0x40,
    Wakeup = 0x50
  }

  private enum FmiLsBusLinFramePart : byte
  {
    Header = 0x1,
    Response = 0x2
  }

  private enum FmiLsBusLinChecksumType : byte
  {
    UnkownChecksum = 0x0,
    ClassicChecksum = 0x1,
    EnhancedChecksum = 0x2
  }

  private enum FmiLsBusLinConfigParameterType : byte
  {
    Baudrate = 0x1,
    NodeType = 0x2
  }

  private enum FmiLsBusLinNodeType : byte
  {
    LinCommander = 0x1,
    LinResponder = 0x2
  }

  private enum OperationHeaderStatus
  {
    Ok,
    IncompleteHeader,
    MalformedLength
  }

  // default ctor if no LIN traffic to manage
  public SilKitLinManager()
  {
    _silKitEntity = null!;
    LinControllers = new Dictionary<uint, LinControllerContext>();
    LinBuffer = new SortedList<ulong, Dictionary<uint, Dictionary<byte, byte[]>>>();
  }

  public SilKitLinManager(SilKitEntity silKitEntity)
  {
    _silKitEntity = silKitEntity;

    LinControllers = new Dictionary<uint, LinControllerContext>();
    LinBuffer = new SortedList<ulong, Dictionary<uint, Dictionary<byte, byte[]>>>();
    if (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      LinBuffer.Add(0, new Dictionary<uint, Dictionary<byte, byte[]>>());
    }
  }

  #region service creation
  public bool CreateLinController(string controllerName, string networkName, uint vRefOut)
  {
    var linController = _silKitEntity.CreateLinController(controllerName, networkName);
    lock (_linControllersLock)
    {
      return LinControllers.TryAdd(vRefOut, new LinControllerContext(linController));
    }
  }

  public void InitLinController(uint vRef, LinControllerMode controllerMode, uint baudRate)
  {
    LinControllerContext controllerContext;
    lock (_linControllersLock)
    {
      if (!LinControllers.TryGetValue(vRef, out controllerContext!))
      {
        throw new NullReferenceException($"No LIN controller found for value reference {vRef}");
      }
    }
    if (controllerMode != LinControllerMode.Master)
    {
      throw new NotSupportedException("Only LIN master mode is supported.");
    }

    InitializeController(controllerContext, baudRate);
  }

  public UInt64 AddFrameStatusHandler(uint vRef, uint vRefIn, LinFrameStatusHandler handler)
  {
    LinControllerContext controllerContext;
    lock (_linControllersLock)
    {
      if (!LinControllers.TryGetValue(vRef, out controllerContext!))
      {
        throw new NullReferenceException($"No LIN controller found for value reference {vRef}");
      }
    }

    return controllerContext.Controller.AddFrameStatusHandler((IntPtr)vRefIn, handler);
  }

  public void SendAllFrames(List<Tuple<uint, byte[]>> linList)
  {
    // Transmit operations with an empty data field that are replaced by a subsequent operation
    // carrying the data for the same LIN ID must not be sent on the SIL Kit network.
    var replacedEmptyOperations = CollectReplacedEmptyOperations(linList);

    foreach (var pairRefOperation in linList)
    {
      var vRef = pairRefOperation.Item1;
      var binary = pairRefOperation.Item2;

      var offset = 0;
      while (offset < binary.Length)
      {
        var headerStatus = TryReadOperationHeader(binary, offset, out var operation, out var operationLength);

        if (headerStatus == OperationHeaderStatus.IncompleteHeader)
        {
          _silKitEntity.Logger.Log(LogLevel.Warn, $"Incomplete LIN Bus Operation header on Tx_Data with value " +
            $"reference {vRef}. {binary.Length - offset} trailing bytes could not be parsed and are ignored.");
          break;
        }

        if (headerStatus == OperationHeaderStatus.MalformedLength)
        {
          _silKitEntity.Logger.Log(LogLevel.Warn, $"Malformed LIN Bus Operation on Tx_Data with value " +
            $"reference {vRef}. The operation Length field is {operationLength} but {binary.Length - offset} " +
            $"bytes remain. The rest of the binary is ignored.");
          break;
        }

        if (IsReplacedEmptyOperation(vRef, binary, offset, operation, operationLength, replacedEmptyOperations))
        {
          offset += (int)operationLength;
          continue;
        }

        var operationBytes = new byte[operationLength];
        Array.Copy(binary, offset, operationBytes, 0, (int)operationLength);

        ProcessOperation(vRef, operation, operationBytes);

        offset += (int)operationLength;
      }
    }
  }

  // Handle the case where a frame with an empty data field is followed by a frame containing the
  // response with the same LIN ID. In that case filter the first operation.
  private static Dictionary<(uint vRef, byte linId), int> CollectReplacedEmptyOperations(
    List<Tuple<uint, byte[]>> linList)
  {
    var replacedCounts = new Dictionary<(uint, byte), int>();
    var pendingEmptyCounts = new Dictionary<(uint, byte), int>();

    foreach (var pairRefOperation in linList)
    {
      var vRef = pairRefOperation.Item1;
      var binary = pairRefOperation.Item2;

      var offset = 0;
      while (offset < binary.Length)
      {
        // malformed data is reported by the dispatching loop of SendAllFrames
        if (TryReadOperationHeader(binary, offset, out var operation, out var operationLength) !=
            OperationHeaderStatus.Ok)
        {
          break;
        }

        if (operation == LinOperations.Transmit &&
            TryReadTransmitFrameInfo(binary, offset, operationLength, out var linId, out var dataLength))
        {
          var key = (vRef, linId);
          if (dataLength == 0)
          {
            pendingEmptyCounts[key] = pendingEmptyCounts.GetValueOrDefault(key) + 1;
          }
          else if (pendingEmptyCounts.GetValueOrDefault(key) > 0)
          {
            replacedCounts[key] = replacedCounts.GetValueOrDefault(key) + pendingEmptyCounts[key];
            pendingEmptyCounts[key] = 0;
          }
        }

        offset += (int)operationLength;
      }
    }

    return replacedCounts;
  }

  // Returns true and consumes one entry of replacedEmptyOperations if the operation is a Transmit
  // operation with an empty data field that is replaced by a later operation with the same LIN ID.
  private bool IsReplacedEmptyOperation(
    uint vRef,
    byte[] binary,
    int offset,
    LinOperations operation,
    uint operationLength,
    Dictionary<(uint vRef, byte linId), int> replacedEmptyOperations)
  {
    if (operation != LinOperations.Transmit ||
        !TryReadTransmitFrameInfo(binary, offset, operationLength, out var linId, out var dataLength) ||
        dataLength != 0)
    {
      return false;
    }

    var key = (vRef, linId);
    if (replacedEmptyOperations.GetValueOrDefault(key) <= 0)
    {
      return false;
    }

    replacedEmptyOperations[key]--;
    _silKitEntity.Logger.Log(LogLevel.Debug, $"Skipped LIN Transmit Operation with an empty data field for LIN ID " +
      $"0x{linId:X2} on Tx_Data with value reference {vRef}. A subsequent operation with the same LIN ID provides " +
      $"the frame response.");

    return true;
  }

  private static OperationHeaderStatus TryReadOperationHeader(
    byte[] binary,
    int offset,
    out LinOperations operation,
    out uint operationLength)
  {
    operation = default;
    operationLength = 0;

    if (binary.Length - offset < 8)
    {
      return OperationHeaderStatus.IncompleteHeader;
    }

    // OP Code : 4 first bytes
    operation = (LinOperations)BinaryPrimitives.ReadUInt32LittleEndian(binary.AsSpan(offset, 4));
    // Length : next 4 bytes
    operationLength = BinaryPrimitives.ReadUInt32LittleEndian(binary.AsSpan(offset + 4, 4));

    if (operationLength < 8 || offset + operationLength > binary.Length)
    {
      return OperationHeaderStatus.MalformedLength;
    }

    return OperationHeaderStatus.Ok;
  }

  private static bool TryReadTransmitFrameInfo(
    byte[] binary,
    int offset,
    uint operationLength,
    out byte linId,
    out byte dataLength)
  {
    linId = 0;
    dataLength = 0;

    if (operationLength < 12)
    {
      return false;
    }

    var length = binary[offset + 11];
    if (length > 8 || operationLength < 12 + length)
    {
      return false;
    }

    linId = binary[offset + 9];
    dataLength = length;

    return true;
  }

  private void ProcessOperation(uint vRef, LinOperations operation, byte[] operationBytes)
  {
    switch (operation)
    {
      case LinOperations.Format_Error:
      {
        _silKitEntity.Logger.Log(LogLevel.Warn, $"Format Error Operation received on Tx_Data with value " +
          $"reference {vRef}. Complete binary data that caused the error: " +
          $"{operationBytes.Skip(10).ToArray()}");
        break;
      }
      case LinOperations.Transmit:
      {
        SendFrame(vRef, operationBytes);
        break;
      }
      case LinOperations.Configuration:
      case LinOperations.Wakeup:
      case LinOperations.Confirm:
      case LinOperations.Bus_Error:
      {
        // unsupported operation
        _silKitEntity.Logger.Log(LogLevel.Warn, $"Unsupported {operation} Operation received on Tx_Data with value " +
          $"reference {vRef}");
        break;
      }
      default:
      {
        // non existing operation
        _silKitEntity.Logger.Log(LogLevel.Warn, $"Non existing Operation received on Tx_Data with value " +
          $"reference {vRef}. Operation code received is: {operation}");
        break;
      }
    }
  }

  public void SendFrame(uint vRef, byte[] data)
  {
    if (data.Length < 12)
    {
      _silKitEntity.Logger.Log(LogLevel.Warn, $"The retrieved LIN operation is malformed. Bytes retrieved: {data}");
      return;
    }

    LinControllerContext controllerContext;
    lock (_linControllersLock)
    {
      if (!LinControllers.TryGetValue(vRef, out controllerContext!))
      {
        _silKitEntity.Logger.Log(LogLevel.Error, $"Trying to send a LIN frame: no LIN controller found for value " +
          $"reference {vRef}");
        return;
      }
    }

    if (!TryParseTransmitOperation(data, out var framePart, out var frame))
    {
      _silKitEntity.Logger.Log(LogLevel.Warn, $"The retrieved LIN operation is malformed. Bytes retrieved: {data}");
      return;
    }

    switch (framePart)
    {
      case FmiLsBusLinFramePart.Header:
        if (frame.dataLength == 0)
        {
          // The header-only case means the master expects a slave/responder participant to
          // provide the response. SIL Kit requires the controller to be explicitly configured
          // to receive (Rx) that LIN ID, otherwise the response is silently dropped and no
          // LIN_RX_OK frame status event is ever raised.
          // Headers that the FMU itself answers in a subsequent operation never reach this point,
          // they are filtered out by SendAllFrames
          EnsureRxFrameResponseConfigured(controllerContext, frame.id);
          controllerContext.Controller.SendFrame(frame, LinFrameResponseType.SlaveResponse);
        }
        else
        {
          controllerContext.Controller.SendFrame(frame, LinFrameResponseType.MasterResponse);
        }
        break;
      case FmiLsBusLinFramePart.Response:
        controllerContext.Controller.SendFrame(frame, LinFrameResponseType.MasterResponse);
        break;
      default:
        _silKitEntity.Logger.Log(LogLevel.Warn, $"Unsupported LIN frame part {(byte)framePart} received on Tx_Data " +
          $"with value reference {vRef}");
        break;
    }
  }

  private void EnsureRxFrameResponseConfigured(LinControllerContext controllerContext, byte linId)
  {
    lock (_linControllersLock)
    {
      if (!controllerContext.ConfiguredRxIds.Add(linId))
      {
        return;
      }
    }

    var response = new LinFrameResponse
    {
      responseMode = LinFrameResponseMode.Rx
    };

    var frame = new LinFrame
    {
      id = linId,
      checksumModel = LinChecksumModel.Unknown,
      dataLength = silKitUnknownDataLength // value 255
    };

    var framePtr = Marshal.AllocHGlobal(Marshal.SizeOf<LinFrame>());
    try
    {
      Marshal.StructureToPtr(frame, framePtr, false);
      response.frame = framePtr;
      controllerContext.Controller.SetFrameResponse(response);
    }
    finally
    {
      Marshal.FreeHGlobal(framePtr);
    }
  }

  private void InitializeController(LinControllerContext controllerContext, uint baudRate)
  {
    var config = new LinControllerConfig
    {
      controllerMode = LinControllerMode.Master,
      baudRate = baudRate,
      numFrameResponses = IntPtr.Zero,
      frameResponses = IntPtr.Zero
    };

    controllerContext.Controller.Init(config);
    _silKitEntity.Logger.Log(LogLevel.Debug, $"Initialized LIN master controller with baudrate {baudRate}.");
  }
  #endregion service creation

  #region data collection & processing
  public void FuncLinFrameStatusHandler(IntPtr context, IntPtr controller, IntPtr frameStatusEvent)
  {
    var vRefIn = (uint)context;
    var linFrameStatusEvent = Marshal.PtrToStructure<LinFrameStatusEvent>(frameStatusEvent);
    var linFrame = Marshal.PtrToStructure<LinFrame>(linFrameStatusEvent.frame);

    var logLevel = linFrameStatusEvent.status == LinFrameStatus.LIN_TX_OK ||
      linFrameStatusEvent.status == LinFrameStatus.LIN_RX_OK
      ? LogLevel.Debug
      : LogLevel.Warn;
    _silKitEntity.Logger.Log(logLevel, $"LIN frame status {linFrameStatusEvent.status} for LIN ID 0x{linFrame.id:X2}, " +
      $"timestamp: {linFrameStatusEvent.timestamp}");

    // Only frames actually received from a Responder participant carry new data that must be
    // forwarded to the FMU. A LIN Commander is expected to receive these RX events for
    // frame responses sent by other (Responder/Slave) SIL Kit participants.
    if (linFrameStatusEvent.status != LinFrameStatus.LIN_RX_OK)
    {
      return;
    }

    // SIL Kit signals an unknown/unavailable data length using value 255,
    // whereas FMI-LS-BUS LIN represents "no data" with dataLength == 0.
    if (linFrame.dataLength == silKitUnknownDataLength)
    {
      linFrame.dataLength = 0;
    }
    var operationBytes = CreateTransmitOperationBytes(linFrame);

    var timeStamp = (_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized) ? 0UL : linFrameStatusEvent.timestamp;

    lock (_linBufferLock)
    {
      if (LinBuffer.TryGetValue(timeStamp, out var refDict))
      {
        if (refDict.TryGetValue(vRefIn, out var idDict))
        {
          idDict[linFrame.id] = operationBytes; // last-is-best per LIN id
        }
        else
        {
          refDict[vRefIn] = new Dictionary<byte, byte[]> { { linFrame.id, operationBytes } };
        }
      }
      else
      {
        var dict = new Dictionary<uint, Dictionary<byte, byte[]>>
        {
          { vRefIn, new Dictionary<byte, byte[]> { { linFrame.id, operationBytes } } }
        };
        LinBuffer.Add(timeStamp, dict);
      }
    }
  }

  public Dictionary<uint, List<byte[]>> RetrieveReceivedLinData(ulong currentTime)
  {
    // set all data that was received up to the current simulation time (~lastSimStep) of the FMU
    var removeCounter = 0;
    var valueUpdates = new Dictionary<uint, List<byte[]>>();
    lock (_linBufferLock)
    {
      foreach (var (timeStamp, linData) in LinBuffer)
      {
        if (!(_silKitEntity.TimeSyncMode == TimeSyncModes.Unsynchronized || timeStamp <= currentTime))
        {
          // no need to iterate future events
          break;
        }
        foreach (var refFramePair in linData)
        {
          if (!valueUpdates.TryGetValue(refFramePair.Key, out var operationList))
          {
            operationList = new List<byte[]>();
            valueUpdates[refFramePair.Key] = operationList;
          }

          foreach (var idDataPair in refFramePair.Value)
          {
            operationList.Add(idDataPair.Value);
          }
        }
        removeCounter++;
      }

      // remove all processed entries from the buffer
      while (removeCounter-- > 0)
      {
        LinBuffer.RemoveAt(0);
      }
    }

    return valueUpdates;
  }

  private static byte[] CreateTransmitOperationBytes(LinFrame linFrame)
  {
    // fmi3LsBusLinOperationTransmit: header (opcode + length, 8 bytes) + framePart (1) + id (1) +
    // checksumType (1) + dataLength (1) + data[dataLength]
    var dataLength = linFrame.dataLength;
    var length = 12 + dataLength;
    var bytes = new byte[length];
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0, 4), (uint)LinOperations.Transmit);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), (uint)length);
    bytes[8] = (byte)FmiLsBusLinFramePart.Response;
    bytes[9] = linFrame.id;
    bytes[10] = linFrame.checksumModel switch
    {
      LinChecksumModel.Classic => (byte)FmiLsBusLinChecksumType.ClassicChecksum,
      LinChecksumModel.Enhanced => (byte)FmiLsBusLinChecksumType.EnhancedChecksum,
      _ => (byte)FmiLsBusLinChecksumType.UnkownChecksum
    };
    bytes[11] = dataLength;
    if (dataLength > 0)
    {
      Array.Copy(linFrame.data, 0, bytes, 12, dataLength);
    }

    return bytes;
  }

  private static bool TryParseTransmitOperation(
    byte[] data,
    out FmiLsBusLinFramePart framePart,
    out LinFrame frame)
  {
    framePart = default;
    frame = new LinFrame();

    if (data.Length < 12)
    {
      return false;
    }

    framePart = (FmiLsBusLinFramePart)data[8];
    var dataLength = data[11];
    if (dataLength > 8 || data.Length < 12 + dataLength)
    {
      return false;
    }

    frame.id = data[9];
    frame.checksumModel = data[10] switch
    {
      (byte)FmiLsBusLinChecksumType.ClassicChecksum => LinChecksumModel.Classic,
      (byte)FmiLsBusLinChecksumType.EnhancedChecksum => LinChecksumModel.Enhanced,
      _ => LinChecksumModel.Unknown
    };
    frame.dataLength = dataLength;
    if (dataLength > 0)
    {
      Array.Copy(data, 12, frame.data, 0, dataLength);
    }

    return true;
  }
  #endregion data collection & processing
}
