// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Can;

public interface ICanController
{
  public string CanNetworkName { get; set; }
  public uint transmitId { get; set; }

  public UInt64 AddFrameHandler(IntPtr context, CanFrameHandler handler, byte directionMask);
  public UInt64 AddFrameTransmitHandler(IntPtr context, CanFrameTransmitHandler handler, Int32 statusMask);
  public void SetBaudRate(UInt32 rate, UInt32 fdRate, UInt32 xlRate);
  public void SendFrame(CanFrame msg, IntPtr userContext);
  public void Start();
  public void Stop();
}
