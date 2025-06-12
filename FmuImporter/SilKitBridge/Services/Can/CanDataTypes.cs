// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services.PubSub;
using System.Runtime.InteropServices;
using static SilKit.SilKitVersion;

namespace SilKit.Services.Can;

public enum TransmitDirection : byte
{
  // Undefined
  Undefined = 0,
  // Transmit
  TX = 1,
  // Receive
  RX = 2,
  // Send/Receive
  TXRX = 3
}
public enum CanTransmitStatus : Int32
{
  //! The message was successfully transmitted on the CAN bus.
  Transmitted = 1 << 0,
  //! The transmit queue was reset. (currently not in use)
  Canceled = 1 << 1,
  //! The transmit request was rejected, because the transmit queue is full.
  TransmitQueueFull = 1 << 2
}

[Flags]
public enum SilKitCanFrameFlag : uint
{
  Ide = 1 << 9,  // Identifier Extension
  Rtr = 1 << 4,  // Remote Transmission Request
  Fdf = 1 << 12, // FD Format Indicator
  Brs = 1 << 13, // Bit Rate Switch (for FD Format only)
  Esi = 1 << 14, // Error State Indicator (for FD Format only)
  Xlf = 1 << 15, // XL Format Indicator
  Sec = 1 << 16  // Simple Extended Content (for XL Format only)
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct CanFrame
{
  public CanFrame()
  {
    structHeader = GetStructHeader(ServiceId.Can, DatatypeId.CanFrame);
    id = 0;
    flags = 0;
    dlc = 0;
    sdt = 0;
    vcid = 0;
    af = 0;
    data = new ByteVector();
  }

  internal StructHeader structHeader;
  public UInt32 id;
  public UInt32 flags;
  public UInt16 dlc;
  public byte sdt;
  public byte vcid;
  public UInt32 af;
  public ByteVector data;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct CanFrameEvent
{
  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public IntPtr frame;
  public TransmitDirection direction;
  public IntPtr? userContext;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct CanFrameTransmitEvent
{
  internal StructHeader structHeader;
  public IntPtr userContext;
  public UInt64 timestampInNs;
  public CanTransmitStatus status;
  public UInt32 canId;
}


[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void CanFrameTransmitHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr frameTransmitEvent);


[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void CanFrameHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr frameEvent);
