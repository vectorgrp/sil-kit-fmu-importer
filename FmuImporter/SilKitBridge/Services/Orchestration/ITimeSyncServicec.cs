// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Orchestration;

public interface ITimeSyncService
{
  public void SetSimulationStepHandler(
    SimulationStepHandler simulationStepHandler,
    UInt64 initialStepSize);
}
