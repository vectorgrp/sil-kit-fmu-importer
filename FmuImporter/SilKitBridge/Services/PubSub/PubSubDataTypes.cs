// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using static SilKit.SilKitVersion;

namespace SilKit.Services.PubSub;

public delegate void DataMessageHandler(
  IntPtr context,
  IDataSubscriber subscriber,
  DataMessageEvent dataMessageEvent);

public struct DataMessageEvent
{
  internal DataMessageEvent(DataMessageEventInternal internalDataMessageEvent)
  {
    TimestampInNS = internalDataMessageEvent.timestampInNs;
    Data = new byte[(int)internalDataMessageEvent.data.size];
    Marshal.Copy(internalDataMessageEvent.data.data, Data, 0, Data.Length);
  }

  public UInt64 TimestampInNS;
  public byte[] Data;
}

// Internal data types
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct DataMessageEventInternal
{
  public DataMessageEventInternal()
  {
    structHeader = GetStructHeader(ServiceId.Data, DatatypeId.DataMessageEvent);
    timestampInNs = 0;
    data = new ByteVector();
  }

  internal StructHeader structHeader;
  public UInt64 timestampInNs;
  public ByteVector data;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ByteVector
{
  public IntPtr data;
  public IntPtr /*size_t*/ size;
}
