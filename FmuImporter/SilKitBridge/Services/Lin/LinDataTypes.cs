// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using static SilKit.SilKitVersion;

namespace SilKit.Services.Lin;

public enum LinDirection : byte
{
  Undefined = 0,
  TX = 1,
  RX = 2,
  TXRX = 3
}

public enum LinControllerStatus : uint
{
  Unknown = 0,
  Operational = 1,
  Sleep = 2,
  SleepPending = 3
}

public enum LinControllerMode : byte
{
  Inactive = 0,
  Master = 1,
  Slave = 2
}

public enum LinFrameResponseMode : byte
{
  Unused = 0,
  Rx = 1,
  TxUnconditional = 2
}

public enum LinChecksumModel : byte
{
  Unknown = 0,
  Enhanced = 1,
  Classic = 2
}

public enum LinFrameResponseType : byte
{
  MasterResponse = 0,
  SlaveResponse = 1,
  SlaveToSlave = 2
}

public enum LinFrameStatus : byte
{
  NOT_OK = 0,
  LIN_TX_OK = 1,
  LIN_TX_BUSY = 2,
  LIN_TX_HEADER_ERROR = 3,
  LIN_TX_ERROR = 4,
  LIN_RX_OK = 5,
  LIN_RX_BUSY = 6,
  LIN_RX_ERROR = 7,
  LIN_RX_NO_RESPONSE = 8
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LinFrame
{
  public LinFrame()
  {
    structHeader = GetStructHeader(ServiceId.Lin, DatatypeId.LinFrame);
    id = 0;
    checksumModel = LinChecksumModel.Unknown;
    dataLength = 0;
    data = new byte[8];
  }

  internal StructHeader structHeader;
  public byte id;
  public LinChecksumModel checksumModel;
  public byte dataLength;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
  public byte[] data;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LinFrameStatusEvent
{
  internal StructHeader structHeader;
  public UInt64 timestamp;
  public IntPtr frame;
  public LinFrameStatus status;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LinWakeupEvent
{
  internal StructHeader structHeader;
  public UInt64 timestamp;
  public LinDirection direction;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LinGoToSleepEvent
{
  internal StructHeader structHeader;
  public UInt64 timestamp;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LinFrameResponse
{
  public LinFrameResponse()
  {
    structHeader = GetStructHeader(ServiceId.Lin, DatatypeId.LinFrameResponse);
    frame = IntPtr.Zero;
    responseMode = LinFrameResponseMode.Unused;
  }

  internal StructHeader structHeader;
  public IntPtr frame;
  public LinFrameResponseMode responseMode;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LinControllerConfig
{
  public LinControllerConfig()
  {
    structHeader = GetStructHeader(ServiceId.Lin, DatatypeId.LinControllerConfig);
    controllerMode = LinControllerMode.Inactive;
    baudRate = 0;
    numFrameResponses = IntPtr.Zero;
    frameResponses = IntPtr.Zero;
  }

  internal StructHeader structHeader;
  public LinControllerMode controllerMode;
  public UInt32 baudRate;
  public IntPtr numFrameResponses;
  public IntPtr frameResponses;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void LinFrameStatusHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr frameStatusEvent);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void LinGoToSleepHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr goToSleepEvent);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void LinWakeupHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr wakeupEvent);
