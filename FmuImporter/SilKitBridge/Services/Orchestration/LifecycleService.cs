using System.Diagnostics;
using System.Runtime.InteropServices;
using static SilKit.Services.Orchestration.LifecycleService;

namespace SilKit.Services.Orchestration
{
  public interface ILifecycleService
  {
    public void StartLifecycle();
    public void Stop(string reason);
    public void WaitForLifecycleToComplete();

    public ITimeSyncService CreateTimeSyncService();

    public void SetCommunicationReadyHandler(CommunicationReadyHandler communicationReadyHandler);
    public void SetStopHandler(StopHandler stopHandler);
    public void SetShutdownHandler(ShutdownHandler shutdownHandler);
  }

  public class LifecycleService : ILifecycleService
  {
    #region nested classes
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct SilKit_LifecycleConfiguration
    {
      public SilKit_LifecycleConfiguration()
      {
        structHeader = SilKitVersion.GetStructHeader(SilKitVersion.ServiceId.Participant, SilKitVersion.DatatypeId.LifecycleConfiguration);
        operationMode = 0;
      }

      internal SilKitVersion.SilKit_StructHeader structHeader;
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

    public interface ITimeSyncService
    {
      public void SetSimulationStepHandler(
          SimulationStepHandler simulationStepHandler,
          UInt64 initialStepSize);

    }
    public class TimeSyncService : ITimeSyncService
    {
      private readonly LifecycleService lifecycleService;
      private IntPtr timeSyncServicePtr;
      internal IntPtr TimeSyncServicePtr
      {
        get { return timeSyncServicePtr; }
        private set { timeSyncServicePtr = value; }
      }

      #region ctor & dtor
      internal TimeSyncService(LifecycleService lifecycleService)
      {
        this.lifecycleService = lifecycleService;
        Helpers.ProcessReturnCode(
          (Helpers.SilKit_ReturnCodes)SilKit_TimeSyncService_Create(
            out timeSyncServicePtr,
            lifecycleService.LifecycleServicePtr),
          System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
      }

      /*
          SilKit_TimeSyncService_Create(
              SilKit_TimeSyncService** outTimeSyncService,
              SilKit_LifecycleService* lifecycleService);
      */
      [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
      private static extern int SilKit_TimeSyncService_Create(
          [Out] out IntPtr outTimeSyncService,
          [In] IntPtr lifecycleService);

      ~TimeSyncService()
      {
        TimeSyncServicePtr = IntPtr.Zero;
      }
      #endregion ctor & dtor

      #region callback handling
      private SimulationStepHandler? simulationStepHandler;
      public void SetSimulationStepHandler(SimulationStepHandler simulationStepHandler, UInt64 initialStepSize)
      {
        this.simulationStepHandler = simulationStepHandler;
        Helpers.ProcessReturnCode(
          (Helpers.SilKit_ReturnCodes)SilKit_TimeSyncService_SetSimulationStepHandler(
            TimeSyncServicePtr, 
            out var context, 
            SimulationStepHandlerInternal, 
            initialStepSize),
          System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
      }
      private void SimulationStepHandlerInternal(IntPtr context, IntPtr timeSyncService, UInt64 now, UInt64 duration)
      {
        // double check if this is the correct lifecycle service
        if (timeSyncService != TimeSyncServicePtr) { return; }
        simulationStepHandler?.Invoke(now, duration);
      }

      /*
          SilKit_TimeSyncService_SetSimulationStepHandler(
              SilKit_TimeSyncService* timeSyncService, void* context, 
              SilKit_TimeSyncService_SimulationStepHandler_t handler, 
              SilKit_NanosecondsTime initialStepSize);
      */
      [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
      private static extern int SilKit_TimeSyncService_SetSimulationStepHandler(
          [In] IntPtr timesyncService,
          [Out] out IntPtr context,
          SilKit_TimeSyncService_SetSimulationStepHandler_t handler,
          UInt64 initialStepSize);
      private delegate void SilKit_TimeSyncService_SetSimulationStepHandler_t(IntPtr context, IntPtr timeSyncService, UInt64 now, UInt64 duration);
      #endregion callback handling
    }
    #endregion nested classes

    private readonly Participant participant;
    private readonly LifecycleConfiguration lifecycleConfiguration;
    private IntPtr lifecycleServicePtr;
    internal IntPtr LifecycleServicePtr
    {
      get { return lifecycleServicePtr; }
      private set { lifecycleServicePtr = value; }
    }

    #region ctor & dtor
    internal LifecycleService(Participant participant, LifecycleConfiguration lc)
    {
      this.participant = participant;
      this.lifecycleConfiguration = lc;
      var internalLc = lc.LifecycleConfigurationInternal;
      
      var handler = GCHandle.Alloc(internalLc, GCHandleType.Pinned);

      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_Create(
          out lifecycleServicePtr, 
          participant.ParticipantPtr, 
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
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_LifecycleService_Create(
        [Out] out IntPtr outLifecycleService,
        [In] IntPtr participant,
        [In] SilKit_LifecycleConfiguration startConfiguration
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
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
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
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
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
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_LifecycleService_WaitForLifecycleToComplete([In] IntPtr lifecycleService, [Out] out IntPtr outParticipantState);


    public ITimeSyncService CreateTimeSyncService()
    {
      return new TimeSyncService(this);
    }

    #region callback handling
    private CommunicationReadyHandler? communicationReadyHandler;
    public void SetCommunicationReadyHandler(CommunicationReadyHandler communicationReadyHandler)
    {
      this.communicationReadyHandler = communicationReadyHandler;
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetCommunicationReadyHandler(
          LifecycleServicePtr, 
          out var context, 
          CommunicationReadyHandlerInternal),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    private void CommunicationReadyHandlerInternal(IntPtr context, IntPtr lifecycleService)
    {
      // double check if this is the correct lifecycle service
      if (lifecycleService != LifecycleServicePtr) { return; }
      communicationReadyHandler?.Invoke();
    }

    /*
        SilKit_LifecycleService_SetCommunicationReadyHandler(
            SilKit_LifecycleService* lifecycleService, 
            void* context,
            SilKit_LifecycleService_CommunicationReadyHandler_t handler);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_LifecycleService_SetCommunicationReadyHandler([In] IntPtr lifecycleService,
                                                               [Out] out IntPtr context,
                                                               SilKit_LifecycleService_StopHandler_t handler);
    private delegate void SilKit_LifecycleService_SetCommunicationReadyHandler_t(IntPtr context, IntPtr lifecycleService);

    private StopHandler? stopHandler;
    public void SetStopHandler(StopHandler stopHandler)
    {
      this.stopHandler = stopHandler;
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetStopHandler(
          LifecycleServicePtr, 
          out var context, 
          StopHandlerInternal),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    private void StopHandlerInternal(IntPtr context, IntPtr lifecycleService)
    {
      if (lifecycleService != LifecycleServicePtr) { return; }
      stopHandler?.Invoke();
    }

    /*
        SilKit_LifecycleService_SetStopHandler(
            SilKit_LifecycleService* lifecycleService,
            void* context,
            SilKit_LifecycleService_StopHandler_t handler);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_LifecycleService_SetStopHandler([In] IntPtr lifecycleService,
                                                               [Out] out IntPtr context,
                                                               SilKit_LifecycleService_StopHandler_t handler);
    private delegate void SilKit_LifecycleService_StopHandler_t(IntPtr context, IntPtr lifecycleService);


    private ShutdownHandler? shutdownHandler;
    public void SetShutdownHandler(ShutdownHandler shutdownHandler)
    {
      this.shutdownHandler = shutdownHandler;
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LifecycleService_SetShutdownHandler(
          LifecycleServicePtr, 
          out var context, 
          ShutdownHandlerInternal),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    private void ShutdownHandlerInternal(IntPtr context, IntPtr lifecycleService)
    {
      // double check if this is the correct lifecycle service
      if (lifecycleService != LifecycleServicePtr) { return; }
      shutdownHandler?.Invoke();
    }

    /* 
        SilKit_LifecycleService_SetShutdownHandler(
            SilKit_LifecycleService* lifecycleService, 
            void* context, 
            SilKit_LifecycleService_ShutdownHandler_t handler);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_LifecycleService_SetShutdownHandler([In] IntPtr lifecycleService,
                                                               [Out] out IntPtr context,
                                                               SilKit_LifecycleService_StopHandler_t handler);
    private delegate void SilKit_LifecycleService_SetShutdownHandler_t(IntPtr context, IntPtr lifecycleService);

    // TODO think about context
    #endregion callback handling
  }
}
