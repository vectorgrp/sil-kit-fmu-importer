// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using System.Text;

namespace SilKit.Supplements;

public class Deserializer
{
  private List<byte> _buffer = new List<byte>();
  private int _readPos;
  private byte _unalignedBits;

  public Deserializer(List<byte> buffer)
  {
    _buffer = buffer;
  }

  public object Deserialize(Type type)
  {
    if (type == typeof(byte))
    {
      var span = DeserializeAligned(sizeof(byte));
      return span[0];
    }

    if (type == typeof(sbyte))
    {
      var span = DeserializeAligned(sizeof(sbyte));
      return (sbyte)span[0];
    }

    if (type == typeof(Int16))
    {
      var span = DeserializeAligned(sizeof(Int16));
      return BitConverter.ToInt16(span);
    }

    if (type == typeof(UInt16))
    {
      var span = DeserializeAligned(sizeof(UInt16));
      return BitConverter.ToUInt16(span);
    }

    if (type == typeof(Int32))
    {
      var span = DeserializeAligned(sizeof(Int32));
      return BitConverter.ToInt32(span);
    }

    if (type == typeof(UInt32))
    {
      var span = DeserializeAligned(sizeof(UInt32));
      return BitConverter.ToUInt32(span);
    }

    if (type == typeof(Int64) || type == typeof(Enum))
    {
      var span = DeserializeAligned(sizeof(Int64));
      return BitConverter.ToInt64(span);
    }

    if (type == typeof(UInt64))
    {
      var span = DeserializeAligned(sizeof(UInt64));
      return BitConverter.ToUInt64(span);
    }

    if (type == typeof(bool))
    {
      return DeserializeAligned(1)[0] != 0;
    }

    if (type == typeof(Single))
    {
      var span = DeserializeAligned(sizeof(Single));
      return BitConverter.ToSingle(span);
    }

    if (type == typeof(Double))
    {
      var span = DeserializeAligned(sizeof(Double));
      return BitConverter.ToDouble(span);
    }

    if (type == typeof(String))
    {
      var size = BeginArray();
      AssertCapacity(size);
      var span = DeserializeAligned(size);
      EndArray();
      return Encoding.ASCII.GetString(span);
    }

    if (type == typeof(byte[]))
    {
      var size = BeginArray();
      AssertCapacity(size);
      var span = DeserializeAligned(size);
      EndArray();
      return span.ToArray();
    }

    throw new NotSupportedException("Failed to deserialize data type: " + type.FullName);
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

  public int BeginArray()
  {
    return (int)BitConverter.ToUInt32(DeserializeAligned(sizeof(UInt32)));
  }

  public void EndArray()
  {
    // NOP
  }

  public bool BeginOptional()
  {
    return (bool)Deserialize(typeof(bool));
  }

  public void EndOptional()
  {
    // NOP
  }

  public void Reset(List<byte> buffer)
  {
    _buffer = buffer;
    _readPos = 0;
    _unalignedBits = 0;
  }


  private Span<byte> DeserializeAligned(int byteCount)
  {
    Align();
    AssertCapacity(byteCount);

    var span = CollectionsMarshal.AsSpan(_buffer).Slice(_readPos, byteCount);
    _readPos += byteCount;

    return span;
  }

  private void Align()
  {
    if (_unalignedBits != 0)
    {
      throw new NotSupportedException(
        "Currently, only byte aligned data serialization is supported (e.g., int8 is ok, but not int7.");
    }
  }

  private void AssertCapacity(int requiredSize)
  {
    if (_buffer.Count - _readPos < requiredSize)
    {
      throw new InvalidOperationException($"{nameof(Deserializer)}::AssertCapacity: end of buffer");
    }
  }
}
