// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.Ethernet;

internal class EthernetController : IEthernetController
{
  public string ControllerName { get; set; }
  public string NetworkName { get; set; }
  public uint TransmitId { get; set; }

  private readonly Participant _participant;
  private readonly IntPtr _dataControllerPtr;

  private EthernetFrameHandler? _ethernetFrameHandler;
  private EthernetFrameTransmitHandler? _ethernetFrameTransmitHandler;

  internal IntPtr DataControllerPtr
  {
    get
    {
      return _dataControllerPtr;
    }
  }

  internal EthernetController(Participant participant, string controllerName, string networkName)
  {
    _participant = participant;
    ControllerName = controllerName;
    NetworkName = networkName;

    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_EthernetController_Create(
      out _dataControllerPtr,
      _participant.ParticipantPtr,
      controllerName,
      networkName));
  }

  /*
    SilKit_EthernetController_Create(
        SilKit_EthernetController** outController,
        SilKit_Participant* participant, 
        const char* name,
        const char* network);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_EthernetController_Create(
    [Out] out IntPtr outController,
    [In] IntPtr participant,
    [In, MarshalAs(UnmanagedType.LPStr)] string name,
    [In, MarshalAs(UnmanagedType.LPStr)] string network);

  public UInt64 AddFrameHandler(IntPtr context, EthernetFrameHandler handler, byte directionMask)
  {
    _ethernetFrameHandler = handler;

    IntPtr outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_EthernetController_AddFrameHandler(
        _dataControllerPtr,
        context,
        _ethernetFrameHandler,
        directionMask,
        outHandlerIdPtr));

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  /*
    SilKit_EthernetController_AddFrameHandler(
        SilKit_EthernetController* controller,
        void* context,
        SilKit_EthernetFrameHandler_t handler,
        SilKit_Direction directionMask,
        SilKit_HandlerId* outHandlerId);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_EthernetController_AddFrameHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] EthernetFrameHandler callback,
    [In] byte directionMask,
    [In] IntPtr outHandlerId);

  public UInt64 AddFrameTransmitHandler(IntPtr context, EthernetFrameTransmitHandler handler, UInt32 transmitStatusMask)
  {
    _ethernetFrameTransmitHandler = handler;

    IntPtr outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_EthernetController_AddFrameTransmitHandler(
          _dataControllerPtr,
          context,
          _ethernetFrameTransmitHandler,
          transmitStatusMask,
          outHandlerIdPtr));

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  /*
    SilKit_EthernetController_AddFrameTransmitHandler(
        SilKit_EthernetController* controller,
        void* context,
        SilKit_EthernetFrameTransmitHandler_t handler,
        SilKit_EthernetTransmitStatus transmitStatusMask,
        SilKit_HandlerId* outHandlerId);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_EthernetController_AddFrameTransmitHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] EthernetFrameTransmitHandler callback,
    [In] UInt32 transmitStatusMask,
    [In] IntPtr outHandlerId);

  public void SendFrame(EthernetFrame frame, IntPtr userContext)
  {
    var ethernetFramePtr = Marshal.AllocHGlobal(Marshal.SizeOf<EthernetFrame>());
    Marshal.StructureToPtr(frame, ethernetFramePtr, false);
    try
    {
      Helpers.ProcessReturnCode(
       (Helpers.SilKit_ReturnCodes)SilKit_EthernetController_SendFrame(
         _dataControllerPtr,
         ethernetFramePtr,
         userContext));
    }
    finally
    {
      Marshal.FreeHGlobal(ethernetFramePtr);
    }
  }

  /*
    SilKit_EthernetController_SendFrame(
        SilKit_EthernetController* controller,
        SilKit_EthernetFrame* frame,
        void* userContext);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_EthernetController_SendFrame(
    [In] IntPtr controller,
    [In] IntPtr frame,
    [In] IntPtr userContext);

  public void Activate()
  {
    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_EthernetController_Activate(_dataControllerPtr));
  }

  /*
    SilKit_EthernetController_Activate(SilKit_EthernetController* controller);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_EthernetController_Activate([In] IntPtr controller);

  public void Deactivate()
  {
    Helpers.ProcessReturnCode(
    (Helpers.SilKit_ReturnCodes)SilKit_EthernetController_Deactivate(_dataControllerPtr));
  }

  /*
    SilKit_EthernetController_Deactivate(SilKit_EthernetController* controller);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_EthernetController_Deactivate([In] IntPtr controller);
}
