// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit.Services.Orchestration;

namespace FmuImporter.SilKit;

public class RealTimeService : ITimeSyncService
{
  // Set by default; null in legacy thread mode (--legacy-thread-mode)
  private readonly FmuExecutionThread? _fmuExecutionThread;

  private ulong _stepSize;
  private SimulationStepHandler? _stepHandler;
  private volatile bool _isRunning;

  private ulong _targetSimTime;

  public RealTimeService(FmuExecutionThread? fmuExecutionThread)
  {
    _fmuExecutionThread = fmuExecutionThread;
  }

  public void SetSimulationStepHandler(SimulationStepHandler simulationStepHandler, ulong initialStepSize)
  {
    _stepHandler = simulationStepHandler;
    _stepSize = initialStepSize;
  }

  public Task Start()
  {
    if (_stepHandler == null)
    {
      throw new Exception("Must call SetSimulationStepHandler before starting.");
    }

    _isRunning = true;

    if (_fmuExecutionThread == null)
    {
      // Legacy thread mode: each step runs on a thread-pool thread
      return Task.Run(
        async () =>
        {
          while (_isRunning)
          {
            await DoStep();
          }
        });
    }

    // Default: the whole step loop runs on the FMU thread
    return _fmuExecutionThread.Post(
      () =>
      {
        while (_isRunning)
        {
          _stepHandler!.Invoke(_targetSimTime, _stepSize);
          _targetSimTime += _stepSize;
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
