// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.Can;

internal class CanController : ICanController
{
  public string CanNetworkName { get; set; }
  public uint transmitId { get; set; }

  private readonly Participant _participant;
  private IntPtr _dataControllerPtr;

  private CanFrameHandler? _canFrameHandler;
  private CanFrameTransmitHandler? _canFrameTransmitHandler;

  internal IntPtr DataControllerPtr
  {
    get
    {
      return _dataControllerPtr;
    }
    private set
    {
      _dataControllerPtr = value;
    }
  }

  internal CanController(Participant participant, string controllerName, string networkName)
  {
    _participant = participant;
    CanNetworkName = networkName;
    transmitId = 0;

    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_CanController_Create(
      out _dataControllerPtr,
      _participant.ParticipantPtr,
      controllerName,
      networkName),
    System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
      SilKit_CanController_Create(
          SilKit_CanController** outController,
          SilKit_Participant* participant, 
          const char* cName,
          const char* cNetwork);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_Create(
    [Out] out IntPtr outController,
    [In] IntPtr participant,
    [In, MarshalAs(UnmanagedType.LPStr)] string cName,
    [In, MarshalAs(UnmanagedType.LPStr)] string cNetwork);

  public UInt64 AddFrameHandler(IntPtr context, CanFrameHandler handler, byte directionMask)
  {
    _canFrameHandler = handler;

    IntPtr outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_CanController_AddFrameHandler(
        _dataControllerPtr,
        context,
        _canFrameHandler,
        directionMask,
        outHandlerIdPtr),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  /*
      SilKit_CanController_AddFrameHandler(
          SilKit_CanController* controller, 
          void* context,
          SilKit_CanFrameHandler_t callback,
          SilKit_Direction directionMask,
          SilKit_HandlerId* outHandlerId);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_AddFrameHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] CanFrameHandler callback,
    [In] byte directionMask,
    [In] IntPtr outHandlerId);

  public UInt64 AddFrameTransmitHandler(IntPtr context, CanFrameTransmitHandler handler, Int32 statusMask)
  {
    _canFrameTransmitHandler = handler;

    IntPtr outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_CanController_AddFrameTransmitHandler(
          _dataControllerPtr,
          context,
          _canFrameTransmitHandler,
          statusMask,
          outHandlerIdPtr),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  /*
      SilKit_CanController_AddFrameTransmitHandler(
          SilKit_CanController* controller,
          void* context,
          SilKit_CanFrameTransmitHandler_t callback,
          SilKit_CanTransmitStatus statusMask,
          SilKit_HandlerId* outHandlerId);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_AddFrameTransmitHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] CanFrameTransmitHandler callback,
    [In] Int32 statusMask,
    [In] IntPtr outHandlerId);

  public void SetBaudRate(UInt32 rate, UInt32 fdRate, UInt32 xlRate)
  {
    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_CanController_SetBaudRate(
      _dataControllerPtr,
      rate,
      fdRate,
      xlRate),
    System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
      SilKit_CanController_SetBaudRate(
          SilKit_CanController* controller, 
          uint32_t rate,
          uint32_t fdRate, 
          uint32_t xlRate);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_SetBaudRate(
    [In] IntPtr controller,
    [In] UInt32 rate,
    [In] UInt32 fdRate,
    [In] UInt32 xlRate);

  public void SendFrame(CanFrame msg, IntPtr userContext)
  {
    var canFramePtr = Marshal.AllocHGlobal(Marshal.SizeOf<CanFrame>());
    Marshal.StructureToPtr(msg, canFramePtr, false);
    try
    {
      Helpers.ProcessReturnCode(
       (Helpers.SilKit_ReturnCodes)SilKit_CanController_SendFrame(
         _dataControllerPtr,
         canFramePtr,
         userContext),
       System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    finally
    {
      Marshal.FreeHGlobal(canFramePtr);
    }
  }

  /*
      SilKit_CanController_SendFrame(
          SilKit_CanController* controller, 
          SilKit_CanFrame* message,
          void* transmitContext);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_SendFrame(
    [In] IntPtr controller,
    [In] IntPtr message,
    [In] IntPtr transmitContext);

  public void Start()
  {
    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_CanController_Start(_dataControllerPtr),
    System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
      SilKit_CanController_Start(SilKit_CanController* controller);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_Start([In] IntPtr controller);

  public void Stop()
  {
    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_CanController_Stop(_dataControllerPtr),
    System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  /*
      SilKit_CanController_Stop(SilKit_CanController* controller);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_CanController_Stop([In] IntPtr controller);
}
