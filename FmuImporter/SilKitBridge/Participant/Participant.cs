// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;
using SilKit.Config;
using SilKit.Services.Can;
using SilKit.Services.Logger;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;

namespace SilKit;

public class Participant : IDisposable
{
  public enum ParticipantStates : short
  {
    /*! An invalid participant state */
    Invalid = 0,

    /*! The controllers created state */
    ServicesCreated = 10,

    /*! The communication initializing state */
    CommunicationInitializing = 20,

    /*! The communication initialized state */
    CommunicationInitialized = 30,

    /*! The initialized state */
    ReadyToRun = 40,

    /*! The running state */
    Running = 50,

    /*! The paused state */
    Paused = 60,

    /*! The stopping state */
    Stopping = 70,

    /*! The stopped state */
    Stopped = 80,

    /*! The error state */
    Error = 90,

    /*! The shutting down state */
    ShuttingDown = 100,

    /*! The shutdown state */
    Shutdown = 110,

    /*! The aborting state */
    Aborting = 120
  }

  internal static ILogger? Logger { get; private set; }

  private IntPtr _participantPtr = IntPtr.Zero;

  internal IntPtr ParticipantPtr
  {
    get
    {
      return _participantPtr;
    }
    private set
    {
      _participantPtr = value;
    }
  }

#region ctor & dtor

  public Participant(ParticipantConfiguration configuration, string participantName, string registryUri)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_Participant_Create(
        out _participantPtr,
        configuration.ParticipantConfigurationPtr,
        participantName,
        registryUri),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    Logger = new Logger(ParticipantPtr);
  }

  /*
      SilKit_Participant_Create(
          SilKit_Participant** outParticipant,
          SilKit_ParticipantConfiguration* participantConfiguration,
          const char* participantName, 
          const char* registryUri);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_Participant_Create(
    [Out] out IntPtr outParticipant,
    [In] IntPtr participantConfiguration,
    [MarshalAs(UnmanagedType.LPStr)] string participantName,
    [MarshalAs(UnmanagedType.LPStr)] string registryUri);


  ~Participant()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    SilKit_Participant_Destroy(ParticipantPtr);
    ParticipantPtr = IntPtr.Zero;
  }

  private bool _disposedValue;

  protected void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
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

  /*
   * SilKit_Participant_Destroy(SilKit_Participant* participant);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_Participant_Destroy([In] IntPtr participant);

#endregion ctor & dtor

  public ILifecycleService CreateLifecycleService(LifecycleService.LifecycleConfiguration lc)
  {
    return new LifecycleService(this, lc);
  }

  public ICanController CreateCanController(string controllerName, string networkName)
  {
    return new CanController(this, controllerName, networkName);
  }

  public IDataPublisher CreateDataPublisher(string controllerName, PubSubSpec dataSpec, byte history)
  {
    return new DataPublisher(this, controllerName, dataSpec, history);
  }

  public IDataSubscriber CreateDataSubscriber(
    string controllerName,
    PubSubSpec dataSpec,
    IntPtr context,
    DataMessageHandler dataMessageHandler)
  {
    return new DataSubscriber(this, controllerName, dataSpec, context, dataMessageHandler);
  }

  public ILogger GetLogger()
  {
    if (Logger == null)
    {
      throw new NullReferenceException("GetLogger failed to retrieve a logger object");
    }

    return Logger;
  }
}
