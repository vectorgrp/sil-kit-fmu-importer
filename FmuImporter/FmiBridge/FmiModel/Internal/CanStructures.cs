// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Fmi.FmiModel.Internal;

public enum CanOperations : uint
{
  Format_Error = 0x01,
  CAN_Transmit = 0x10,
  CAN_FD_Transmit = 0x11,
  CAN_XL_Transmit = 0x12,
  Confirm = 0x20,
  Arbitration_Lost = 0x30,
  Bus_Error = 0x31,
  Configuration = 0x40,
  Status = 0x41,
  Wakeup = 0x42
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class TransmitOperation
{
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
  public byte[] OPCode;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
  public byte[] Length;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
  public byte[] ID;
  public byte Ide;

  public TransmitOperation()
  {
    OPCode = new byte[4];
    Length = new byte[4];
    ID = new byte[4];
  }

  public void SetOPCode(Int32 opCode)
  {
    BinaryPrimitives.WriteInt32LittleEndian(OPCode, opCode);
  }

  public void SetLength(UInt32 length)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(Length, length);
  }

  public UInt32 GetLength()
  {
    return BinaryPrimitives.ReadUInt32LittleEndian(Length);
  }

  public void SetID(UInt32 id)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(ID, id);
  }

  public UInt32 GetID()
  {
    return BinaryPrimitives.ReadUInt32LittleEndian(ID);
  }

  public void SetIde(int ide)
  {
    Ide = (byte)ide;
  }

  public int GetIde()
  {
    return Ide;
  }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class CanTransmitOperation : TransmitOperation
{
  public byte Rtr;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
  public byte[] DataLength;
  // Data has an unknown size
  public IntPtr Data;

  // parameterless ctor used for marshalling
  public CanTransmitOperation() : base()
  {
    DataLength = new byte[2];
  }

  public CanTransmitOperation(int dataSize) : this()
  {
    Data = Marshal.AllocHGlobal(dataSize);
    SetDataLength((UInt16)dataSize);
  }

  public void SetDataLength(UInt16 dataLength)
  {
    BinaryPrimitives.WriteUInt16LittleEndian(DataLength, dataLength);
  }

  public UInt16 GetDataLength()
  {
    return BinaryPrimitives.ReadUInt16LittleEndian(DataLength);
  }

  public void SetData(IntPtr data)
  {
    Data = data;
  }

  public IntPtr GetData()
  {
    return Data;
  }

  public void SetRtr(int rtr)
  {
    Rtr = (byte)rtr;
  }

  public UInt32 GetRtr()
  {
    return Rtr;
  }

  public byte[] GetBytes()
  {
    var size = GetDataLength();
    var bytes = OPCode.Concat(Length).Concat(ID).Concat(new byte[] { Ide }).Concat(new byte[] { Rtr }).Concat(DataLength);

    if (size == 0)
    {
      return bytes.ToArray();
    }

    byte[] byteArray = new byte[size];
    Marshal.Copy(Data, byteArray, 0, size);
    return bytes.Concat(byteArray).ToArray();
  }
}
