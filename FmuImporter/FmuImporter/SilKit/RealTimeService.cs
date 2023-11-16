// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services.Orchestration;

namespace FmuImporter.SilKit;

public class RealTimeService : ITimeSyncService
{
  private ulong _stepSize;
  private SimulationStepHandler? _stepHandler;
  private bool _isRunning;

  private ulong _targetSimTime;

  public void SetSimulationStepHandler(SimulationStepHandler simulationStepHandler, ulong initialStepSize)
  {
    _stepHandler = simulationStepHandler;
    _stepSize = initialStepSize;
  }

  public void Start()
  {
    if (_stepHandler == null)
    {
      throw new Exception("Must call SetSimulationStepHandler before starting.");
    }

    _isRunning = true;

    Task.Run(
      async () =>
      {
        while (_isRunning)
        {
          await DoStep();
        }
      });
  }

  public void Stop()
  {
    _isRunning = false;
  }

  private async Task DoStep()
  {
    await Task.Run(
      () =>
      {
        _stepHandler!.Invoke(_targetSimTime, _stepSize);
        _targetSimTime += _stepSize;
      });
  }
}
