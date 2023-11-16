// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SilKit.Services.Orchestration;

public class LifecycleService : ILifecycleService
{
#region nested classes

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal struct SilKit_LifecycleConfiguration
  {
    public SilKit_LifecycleConfiguration()
    {
      structHeader = SilKitVersion.GetStructHeader(
        SilKitVersion.ServiceId.Participant,
        SilKitVersion.DatatypeId.LifecycleConfiguration);
      operationMode = 0;
    }

    internal SilKitVersion.StructHeader structHeader;
    internal byte /*SilKit_OperationMode*/ operationMode;
  }

  public class LifecycleConfiguration
  {
    internal SilKit_LifecycleConfiguration LifecycleConfigurationInternal { get; }

    public enum Modes : byte
    {
      Coordinated = 10,
      Autonomous = 20
    }

    public LifecycleConfiguration(Modes mode)
    {
      LifecycleConfigurationInternal = new SilKit_LifecycleConfiguration()
      {
        operationMode = (byte)mode
      };
    }
  }

  public class TimeSyncService : ITimeSyncService
  {
    private readonly LifecycleService _lifecycleService;
    private IntPtr _timeSyncServicePtr;

    internal IntPtr TimeSyncServicePtr
    {
      get
      {
        return _timeSyncServicePtr;
      }
      private set
      {
        _timeSyncServicePtr = value;
      }
    }

#region ctor & dtor

    internal TimeSyncService(LifecycleService lifecycleService)
    {
      _simStepHandlerDelegate = SimulationStepHandlerInternal;

      _lifecycleService = lifecycleService;
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_TimeSyncService_Create(
          out _timeSyncServicePtr,
          _lifecycleService.LifecycleServicePtr),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }

    /*
        SilKit_TimeSyncService_Create(
            SilKit_TimeSyncService** outTimeSyncService,
            SilKit_LifecycleService* lifecycleService);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_TimeSyncService_Create(
      [Out] out IntPtr outTimeSyncService,
      [In] IntPtr lifecycleService);

    ~TimeSyncService()
    {
      TimeSyncServicePtr = IntPtr.Zero;
    }

#endregion ctor & dtor

#region callback handling

    private readonly SimulationStepHandler _simStepHandlerDelegate;

    private Orchestration.SimulationStepHandler? _simulationStepHandler;

    public void SetSimulationStepHandler(
      Orchestration.SimulationStepHandler simulationStepHandler,
      UInt64 initialStepSize)
    {
      _simulationStepHandler = simulationStepHandler;
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_TimeSyncService_SetSimulationStepHandler(
          TimeSyncServicePtr,
          out var context,
          _simStepHandlerDelegate,
          initialStepSize),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }

    private void SimulationStepHandlerInternal(IntPtr context, IntPtr timeSyncService, UInt64 now, UInt64 duration)
    {
      // double check if this is the correct lifecycle service
      if (timeSyncService != TimeSyncServicePtr)
      {
        return;
      }

      _simulationStepHandler?.Invoke(now, duration);
    }

    /*
        SilKit_TimeSyncService_SetSimulationStepHandler(
            SilKit_TimeSyncService* timeSyncService, void* context, 
            SilKit_TimeSyncService_SimulationStepHandler_t handler, 
            SilKit_NanosecondsTime initialStepSize);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_TimeSyncService_SetSimulationStepHandler(
      [In] IntPtr timesyncService,
      [Out] out IntPtr context,
      SimulationStepHandler handler,
      UInt64 initialStepSize);

    private delegate void SimulationStepHandler(
      IntPtr context,
      IntPtr timeSyncService,
      UInt64 now,
      UInt64 duration);

#endregion callback handling
  }

#endregion nested classes

  private readonly Participant _participant;
  private readonly LifecycleConfiguration _lifecycleConfiguration;
  private IntPtr _lifecycleServicePtr;

  internal IntPtr LifecycleServicePtr
  {
    get
    {
      return _lifecycleServicePtr;
    }
    private set
    {
      _lifecycleServicePtr = value;
    }
  }

  private readonly CommunicationReadyHandler _communicationReadyHandlerDelegate;
  private readonly StartingHandler _startingHandlerDelegate;
  private readonly StopHandler _stopHandlerDelegate;
  private readonly ShutdownHandler _shutdownHandlerDelegate;

#region ctor & dtor

  internal LifecycleService(Participant participant, LifecycleConfiguration lc)
  {
    _communicationReadyHandlerDelegate = CommunicationReadyHandlerInternal;
    _startingHandlerDelegate = StartingHandlerInternal;
    _stopHandlerDelegate = StopHandlerInternal;
    _shutdownHandlerDelegate = ShutdownHandlerInternal;

    _participant = participant;
    _lifecycleConfiguration = lc;
    var internalLc = _lifecycleConfiguration.LifecycleConfigurationInternal;

    var handler = GCHandle.Alloc(internalLc, GCHandleType.Pinned);

    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_Create(
        out _lifecycleServicePtr,
        _participant.ParticipantPtr,
        handler.AddrOfPinnedObject()),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    handler.Free();
  }

  /*
      SilKit_LifecycleService_Create(
          SilKit_LifecycleService** outLifecycleService,
          SilKit_Participant* participant,
          const SilKit_LifecycleConfiguration* startConfiguration);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_Create(
    [Out] out IntPtr outLifecycleService,
    [In] IntPtr participant,
    [In] IntPtr startConfiguration
  );

  ~LifecycleService()
  {
    LifecycleServicePtr = IntPtr.Zero;
  }

#endregion ctor & dtor

  public void StartLifecycle()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_StartLifecycle(LifecycleServicePtr),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
      SilKit_LifecycleService_StartLifecycle(
          SilKit_LifecycleService* lifecycleService);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_StartLifecycle(
    [In] IntPtr lifecycleService);

  public void Stop(string reason)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_Stop(LifecycleServicePtr, reason),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
      SilKit_LifecycleService_Stop(
          SilKit_LifecycleService* lifecycleService, 
          const char* reason);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_Stop(
    [In] IntPtr lifecycleService,
    [MarshalAs(UnmanagedType.LPStr)] string reason);


  public void WaitForLifecycleToComplete()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_WaitForLifecycleToComplete(
        LifecycleServicePtr,
        out var finalState),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    if ((short)finalState != 110)
    {
      Debug.WriteLine($"Finished SIL Kit with state '{finalState}'");
    }
  }

  /*
      SilKit_LifecycleService_WaitForLifecycleToComplete(
          SilKit_LifecycleService* lifecycleService, 
          SilKit_ParticipantState* outParticipantState);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_WaitForLifecycleToComplete(
    [In] IntPtr lifecycleService,
    [Out] out IntPtr outParticipantState);


  public ITimeSyncService CreateTimeSyncService()
  {
    return new TimeSyncService(this);
  }

#region callback handling

  private Orchestration.CommunicationReadyHandler? _communicationReadyHandler;

  public void SetCommunicationReadyHandler(Orchestration.CommunicationReadyHandler communicationReadyHandler)
  {
    _communicationReadyHandler = communicationReadyHandler;
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetCommunicationReadyHandler(
        LifecycleServicePtr,
        out var context,
        _communicationReadyHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  private void CommunicationReadyHandlerInternal(IntPtr context, IntPtr lifecycleService)
  {
    // double check if this is the correct lifecycle service
    if (lifecycleService != LifecycleServicePtr)
    {
      return;
    }

    _communicationReadyHandler?.Invoke();
  }

  /*
      SilKit_LifecycleService_SetCommunicationReadyHandler(
          SilKit_LifecycleService* lifecycleService, 
          void* context,
          SilKit_LifecycleService_CommunicationReadyHandler_t handler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_SetCommunicationReadyHandler(
    [In] IntPtr lifecycleService,
    [Out] out IntPtr context,
    CommunicationReadyHandler handler);

  private delegate void CommunicationReadyHandler(
    IntPtr context,
    IntPtr lifecycleService);

  private Orchestration.StartingHandler? _startingHandler;

  public void SetStartingHandler(Orchestration.StartingHandler startingHandler)
  {
    _startingHandler = startingHandler;
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetStartingHandler(
        LifecycleServicePtr,
        out _,
        _startingHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  private void StartingHandlerInternal(IntPtr context, IntPtr lifecycleService)
  {
    // double check if this is the correct lifecycle service
    if (lifecycleService != LifecycleServicePtr)
    {
      return;
    }

    _startingHandler?.Invoke();
  }

  private delegate void StartingHandler(
    IntPtr context,
    IntPtr lifecycleService);

  /*
      SilKit_LifecycleService_SetStartingHandler(
          SilKit_LifecycleService* lifecycleService, void* context, SilKit_LifecycleService_StartingHandler_t handler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_SetStartingHandler(
    [In] IntPtr lifecycleService,
    [Out] out IntPtr context,
    StartingHandler handler
  );

  private Orchestration.StopHandler? _stopHandler;

  public void SetStopHandler(Orchestration.StopHandler stopHandler)
  {
    _stopHandler = stopHandler;
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetStopHandler(
        LifecycleServicePtr,
        out var context,
        _stopHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  private void StopHandlerInternal(IntPtr context, IntPtr lifecycleService)
  {
    if (lifecycleService != LifecycleServicePtr)
    {
      return;
    }

    _stopHandler?.Invoke();
  }

  /*
      SilKit_LifecycleService_SetStopHandler(
          SilKit_LifecycleService* lifecycleService,
          void* context,
          SilKit_LifecycleService_StopHandler_t handler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_SetStopHandler(
    [In] IntPtr lifecycleService,
    [Out] out IntPtr context,
    StopHandler handler);

  private delegate void StopHandler(IntPtr context, IntPtr lifecycleService);


  private Orchestration.ShutdownHandler? _shutdownHandler;

  public void SetShutdownHandler(Orchestration.ShutdownHandler shutdownHandler)
  {
    _shutdownHandler = shutdownHandler;
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetShutdownHandler(
        LifecycleServicePtr,
        out var context,
        _shutdownHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  private void ShutdownHandlerInternal(IntPtr context, IntPtr lifecycleService)
  {
    // double check if this is the correct lifecycle service
    if (lifecycleService != LifecycleServicePtr)
    {
      return;
    }

    _shutdownHandler?.Invoke();
  }

  /* 
      SilKit_LifecycleService_SetShutdownHandler(
          SilKit_LifecycleService* lifecycleService, 
          void* context, 
          SilKit_LifecycleService_ShutdownHandler_t handler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LifecycleService_SetShutdownHandler(
    [In] IntPtr lifecycleService,
    [Out] out IntPtr context,
    ShutdownHandler handler);

  private delegate void ShutdownHandler(IntPtr context, IntPtr lifecycleService);

#endregion callback handling
}
