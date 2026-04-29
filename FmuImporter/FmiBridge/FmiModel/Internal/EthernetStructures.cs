// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Fmi.FmiModel.Internal;

public enum EthernetOperations : uint
{
  Format_Error = 0x01,
  Transmit = 0x10,
  Confirm = 0x20,
  Bus_Error = 0x30,
  Configuration = 0x40,
  Wakeup = 0x41
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class EthernetTransmitOperation
{
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
  public byte[] OPCode;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
  public byte[] Length;
  public byte StartDelimiter;
  public byte FragmentCounter;
  public byte LastFragment;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
  public byte[] DestinationAddress;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
  public byte[] SourceAddress;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
  public byte[] TypeLength;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
  public byte[] DataLength;
  // Data has an unknown size
  public IntPtr Data;

  public EthernetTransmitOperation()
  {
    OPCode = new byte[4];
    Length = new byte[4];
    StartDelimiter = 0xD5; // default value for ordinary Ethernet frames
    FragmentCounter = 0; // not a continuation mPacket
    LastFragment = 1; // true if full Ethernet frame
    DestinationAddress = new byte[6];
    SourceAddress = new byte[6];
    TypeLength = new byte[2];
    DataLength = new byte[4];
  }

  public EthernetTransmitOperation(int dataSize) : this()
  {
    Data = Marshal.AllocHGlobal(dataSize);
    SetDataLength(dataSize);
  }

  public void SetOPCode(Int32 opCode)
  {
    BinaryPrimitives.WriteInt32LittleEndian(OPCode, opCode);
  }

  public void SetLength(UInt32 length)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(Length, length);
  }

  public void SetDestinationAddress(byte[] address)
  {
    if (address.Length != 6)
    {
      throw new ArgumentException("Ethernet address must be 6 bytes long");
    }
    Array.Copy(address, DestinationAddress, 6);
  }

  public byte[] GetDestinationAddress()
  {
    return DestinationAddress;
  }
  public void SetSourceAddress(byte[] address)
  {
    if (address.Length != 6)
    {
      throw new ArgumentException("Ethernet address must be 6 bytes long");
    }
    Array.Copy(address, SourceAddress, 6);
  }
  public byte[] GetSourceAddress()
  {
    return SourceAddress;
  }
  public void SetTypeLength(byte[] typeLength)
  {
    if (typeLength.Length != 2)
    {
      throw new ArgumentException("Ethernet TypeLength must be 2 bytes long");
    }
    Array.Copy(typeLength, TypeLength, 2);
  }
  public byte[] GetTypeLength()
  {
    return TypeLength;
  }

  public void SetDataLength(Int32 dataLength)
  {
    BinaryPrimitives.WriteInt32LittleEndian(DataLength, dataLength);
  }

  public Int32 GetDataLength()
  {
    return BinaryPrimitives.ReadInt32LittleEndian(DataLength);
  }

  public void SetData(byte[] bytes)
  {
    if (Data != IntPtr.Zero)
    {
      Marshal.FreeHGlobal(Data);
      Data = IntPtr.Zero;
    }

    Data = Marshal.AllocHGlobal(GetDataLength());
    Marshal.Copy(bytes, 0, Data, GetDataLength());
  }

  public byte[] GetData()
  {
    var size = GetDataLength();
    byte[] byteArray = new byte[size];
    Marshal.Copy(Data, byteArray, 0, size);
    return byteArray;
  }

  public byte[] GetBytes()
  {
    var bytes = OPCode.Concat(Length).Concat(new byte[] { StartDelimiter }).Concat(new byte[] { FragmentCounter })
      .Concat(new byte[] { LastFragment }).Concat(DestinationAddress).Concat(SourceAddress).Concat(TypeLength)
      .Concat(DataLength).Concat(GetData());

    return bytes.ToArray();
  }
}
