// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;

namespace SilKit.Supplements;

public class Serializer
{
  private List<byte> _buffer = new List<byte>();
  private byte _unalignedBits;

  // uint8..uint64; int8..int64
  public void Serialize(byte data)
  {
    SerializeInt(new[] { data }, sizeof(byte) * 8);
  }

  public void Serialize(sbyte data)
  {
    SerializeInt(new[] { (byte)data }, sizeof(sbyte) * 8);
  }

  public void Serialize(ushort data)
  {
    SerializeInt(BitConverter.GetBytes(data), sizeof(ushort) * 8);
  }

  public void Serialize(short data)
  {
    SerializeInt(BitConverter.GetBytes(data), sizeof(short) * 8);
  }

  public void Serialize(uint data)
  {
    SerializeInt(BitConverter.GetBytes(data), sizeof(uint) * 8);
  }

  public void Serialize(int data)
  {
    SerializeInt(BitConverter.GetBytes(data), sizeof(int) * 8);
  }

  public void Serialize(ulong data)
  {
    SerializeInt(BitConverter.GetBytes(data), sizeof(ulong) * 8);
  }

  public void Serialize(long data)
  {
    SerializeInt(BitConverter.GetBytes(data), sizeof(long) * 8);
  }

  // boolean
  public void Serialize(bool data)
  {
    SerializeAligned(new[] { data ? (byte)1 : (byte)0 });
  }

  // float; double
  public void Serialize(float data)
  {
    SerializeFloat(BitConverter.GetBytes(data));
  }

  public void Serialize(double data)
  {
    SerializeFloat(BitConverter.GetBytes(data));
  }

  // string
  public void Serialize(string data)
  {
    var utf8String = Encoding.UTF8.GetBytes(data);
    SerializeAligned(BitConverter.GetBytes(utf8String.Length));
    _buffer.Capacity += utf8String.Length;
    _buffer.AddRange(utf8String);
  }

  // byte array & byte list
  public void Serialize(byte[] data)
  {
    SerializeAligned(BitConverter.GetBytes(data.Length));
    _buffer.Capacity += data.Length;
    _buffer.AddRange(data);
  }
  
  // vcdl struct
  public void SerializeRaw(byte[] data)
  {
    Align();
    _buffer.AddRange(data);
  }

  // struct
  public void BeginStruct()
  {
    Align();
  }

  public void EndStruct()
  {
    // NOP
  }

  public void BeginArray(int size)
  {
    SerializeAligned(BitConverter.GetBytes((uint)size));
  }

  public void EndArray()
  {
    // NOP
  }

  public void BeginOptional(bool isAvailable)
  {
    Serialize(isAvailable);
  }

  public void EndOptional()
  {
    // NOP
  }

  public void Reset()
  {
    _buffer = new List<byte>(0);
    _unalignedBits = 0;
  }

  public List<byte> ReleaseBuffer()
  {
    Align();
    var result = _buffer;
    Reset();
    return result;
  }


  private void SerializeInt(byte[] data, int bitSize)
  {
    var remainingBits = bitSize % 8;

    if (remainingBits > 0)
    {
      // SerializeUnaligned(data, bitSize);
      throw new NotSupportedException(
        "Currently, only byte aligned data serialization is supported (e.g., int8 is ok, but not int7.");
    }

    SerializeAligned(data);
  }

  private void SerializeFloat(byte[] data)
  {
    Align();
    var oldSize = _buffer.Count;
    _buffer.Capacity = oldSize + data.Length;
    _buffer.AddRange(data);
  }

  private void SerializeAligned(byte[] data)
  {
    Align();
    var oldSize = _buffer.Count;
    _buffer.Capacity = oldSize + data.Length;
    _buffer.AddRange(data);
  }

  private void Align()
  {
    if (_unalignedBits != 0)
    {
      throw new NotSupportedException(
        "Currently, only byte aligned data serialization is supported (e.g., int8 is ok, but not int7.");
    }
  }
}
