// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using static SilKit.SilKitVersion;

namespace SilKit.Services.Ethernet;

public enum Direction : byte
{
  // Undefined
  Undefined = 0,
  // Transmit (Send)
  TX = 1,
  // Receive
  RX = 2,
  // Send/Receive
  TXRX = 3
}

[Flags]
public enum EthernetTransmitStatus : UInt32
{
  //! The message was successfully transmitted on the Ethernet bus.
  Transmitted = 1 << 0,
  //! The transmit request was rejected, because the Ethernet controller is not active.
  ControllerInactive = 1 << 1,
  //! The transmit request was rejected, because the Ethernet link is down.
  LinkDown = 1 << 2,
  //! The transmit request was dropped, because the transmit queue is full.
  Dropped = 1 << 3,
  // BIT(4) is RESERVED
  //! The given raw Ethernet frame is ill formatted (e.g. frame length is too small or too large, etc.).
  InvalidFrameFormat = 1 << 5
}

public enum EthernetState : UInt32
{
  //! The Ethernet controller is switched off (default after reset).
  Inactive = 0,
  //! The Ethernet controller is active, but a link to another Ethernet controller is not yet established.
  LinkDown = 1,
  //! The Ethernet controller is active and the link to another Ethernet controller is established.
  LinkUp = 2
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EthernetFrame
{
  public EthernetFrame()
  {
    structHeader = GetStructHeader(ServiceId.Ethernet, DatatypeId.EthernetFrame);
    raw = new ByteVector();
  }

  internal StructHeader structHeader;
  public ByteVector raw;

  // Ethernet frame layout:
  // [0-5]   Destination MAC address (6 bytes)
  // [6-11]  Source MAC address (6 bytes)
  // [12-13] EtherType (2 bytes, little-endian)
  // [14-n]  Payload data

  public const int EthernetHeaderSize = 14;

  public byte[] GetDestinationAddress()
  {
    byte[] destAddress = new byte[6];
    Marshal.Copy(raw.data, destAddress, 0, 6);
    return destAddress;
  }

  public byte[] GetSourceAddress()
  {
    byte[] srcAddress = new byte[6];
    Marshal.Copy(raw.data + 6, srcAddress, 0, 6);
    return srcAddress;
  }

  public byte[] GetEtherType()
  {
    byte[] etherTypeBytes = new byte[2];
    Marshal.Copy(raw.data + 12, etherTypeBytes, 0, 2);
    // EtherType is little-endian
    return etherTypeBytes;
  }

  public byte[] GetPayload()
  {
    var payloadLength = (int)raw.size - EthernetHeaderSize;
    if (payloadLength <= 0)
    {
      return Array.Empty<byte>();
    }

    byte[] payload = new byte[payloadLength];
    Marshal.Copy(raw.data + EthernetHeaderSize, payload, 0, payloadLength);
    return payload;
  }
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EthernetFrameEvent
{
  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public IntPtr ethernetFrame;
  public Direction direction;
  public IntPtr userContext;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EthernetFrameTransmitEvent
{
  internal StructHeader structHeader;
  public IntPtr userContext;
  public UInt64 timestampInNs;
  public EthernetTransmitStatus status;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EthernetStateChangeEvent
{
  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public EthernetState state;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EthernetBitrateChangeEvent
{
  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public UInt32 bitrate;  // Bitrate in kBit/sec
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void EthernetFrameHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr frameEvent);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void EthernetFrameTransmitHandler(
  IntPtr context,
  IntPtr controller,
  IntPtr frameTransmitEvent);
