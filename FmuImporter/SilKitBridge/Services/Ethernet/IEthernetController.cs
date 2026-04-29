// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Ethernet;

public interface IEthernetController
{
  public string ControllerName { get; set; }
  public string NetworkName { get; set; }
  public uint TransmitId { get; set; }
  public UInt64 AddFrameHandler(IntPtr context, EthernetFrameHandler handler, byte directionMask);
  public UInt64 AddFrameTransmitHandler(IntPtr context, EthernetFrameTransmitHandler handler, UInt32 transmitStatusMask);
  public void SendFrame(EthernetFrame frame, IntPtr userContext);
  public void Activate();
  public void Deactivate();
}
