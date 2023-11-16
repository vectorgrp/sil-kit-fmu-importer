// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using SilKit;
using SilKit.Config;
using SilKit.Services.Logger;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;
using SilKit.Supplements.VendorData;

namespace FmuImporter.SilKit;

public enum TimeSyncModes
{
  Synchronized,
  Unsynchronized
}

public enum PacingModes
{
  AsFastAsPossible,
  WallClock
}

public class SilKitEntity : IDisposable
{
  private readonly Participant _participant;
  private readonly ILifecycleService _lifecycleService;
  private readonly ITimeSyncService _timeSyncService;
  public TimeSyncModes TimeSyncMode { get; }
  public ILogger Logger { get; set; }
  public PacingModes PacingMode { get; }

  public SilKitEntity(
    string? configurationPath,
    string participantName,
    LifecycleService.LifecycleConfiguration.Modes lifecycleMode,
    TimeSyncModes timeSyncMode,
    PacingModes pacingMode)
  {
    TimeSyncMode = timeSyncMode;
    PacingMode = pacingMode;

    var wrapper = SilKitWrapper.Instance;
    ParticipantConfiguration config;
    if (string.IsNullOrEmpty(configurationPath))
    {
      config = wrapper.GetConfigurationFromString("");
    }
    else
    {
      config = wrapper.GetConfigurationFromFile(configurationPath);
    }

    var lc = new LifecycleService.LifecycleConfiguration(lifecycleMode);

    _participant = wrapper.CreateParticipant(config, participantName);
    _lifecycleService = _participant.CreateLifecycleService(lc);
    switch (timeSyncMode)
    {
      case TimeSyncModes.Synchronized:
        _timeSyncService = _lifecycleService.CreateTimeSyncService();
        break;
      case TimeSyncModes.Unsynchronized:
        _timeSyncService = new RealTimeService();
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(timeSyncMode), timeSyncMode, "Invalid time synchronization mode.");
    }

    _lifecycleService.SetStartingHandler(OnSimulationStarting);

    // get logger
    Logger = _participant.GetLogger();
    config.Dispose();
  }

#region service creation

  public IDataPublisher CreateDataPublisher(string serviceName, string topicName, byte historySize)
  {
    var dataSpec = new PubSubSpec(topicName, Vector.MediaTypeData);

    return _participant.CreateDataPublisher(serviceName, dataSpec, historySize);
  }

  public IDataSubscriber CreateDataSubscriber(
    string serviceName,
    string topicName,
    IntPtr context,
    DataMessageHandler handler)
  {
    var dataSpec = new PubSubSpec(topicName, Vector.MediaTypeData);

    return _participant.CreateDataSubscriber(
      serviceName,
      dataSpec,
      context,
      handler);
  }

#endregion service creation

#region simulation control

  public void StartSimulation(SimulationStepHandler simulationStepHandler, ulong initialStepSizeInNs)
  {
    _timeSyncService.SetSimulationStepHandler(simulationStepHandler, initialStepSizeInNs);
    _lifecycleService.StartLifecycle();
  }

  public void WaitForLifecycleToComplete()
  {
    _lifecycleService.WaitForLifecycleToComplete();
  }

  public void StopSimulation(string reason)
  {
    _lifecycleService.Stop(reason);
    if (TimeSyncMode == TimeSyncModes.Unsynchronized)
    {
      ((RealTimeService)_timeSyncService).Stop();
    }
  }

  private void OnSimulationStarting()
  {
    // only occurs if virtual time synchronization is inactive
    ((RealTimeService)_timeSyncService).Start();
  }

#endregion simulation control

#region IDisposable

  ~SilKitEntity()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
  }

  private bool _disposedValue;

  protected void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
        _participant.Dispose();
      }

      ReleaseUnmanagedResources();
      _disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

#endregion
}
