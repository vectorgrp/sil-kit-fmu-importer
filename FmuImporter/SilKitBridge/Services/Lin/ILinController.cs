// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Lin;

public interface ILinController
{
  public string ControllerName { get; set; }
  public string NetworkName { get; set; }

  public void Init(LinControllerConfig config);
  public void SetFrameResponse(LinFrameResponse response);
  public LinControllerStatus Status();
  public UInt64 AddFrameStatusHandler(IntPtr context, LinFrameStatusHandler handler);
  public UInt64 AddGoToSleepHandler(IntPtr context, LinGoToSleepHandler handler);
  public UInt64 AddWakeupHandler(IntPtr context, LinWakeupHandler handler);
  public void SendFrame(LinFrame frame, LinFrameResponseType responseType);
  public void SendFrameHeader(byte linId);
  public void UpdateTxBuffer(LinFrame frame);
  public void GoToSleep();
  public void GoToSleepInternal();
  public void Wakeup();
  public void WakeupInternal();
}
