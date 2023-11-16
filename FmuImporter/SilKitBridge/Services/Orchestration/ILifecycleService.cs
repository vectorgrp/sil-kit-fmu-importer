// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Orchestration;

public interface ILifecycleService
{
  public void StartLifecycle();
  public void Stop(string reason);
  public void WaitForLifecycleToComplete();

  public ITimeSyncService CreateTimeSyncService();

  public void SetCommunicationReadyHandler(CommunicationReadyHandler communicationReadyHandler);
  public void SetStartingHandler(StartingHandler startingHandler);
  public void SetStopHandler(StopHandler stopHandler);
  public void SetShutdownHandler(ShutdownHandler shutdownHandler);
}
