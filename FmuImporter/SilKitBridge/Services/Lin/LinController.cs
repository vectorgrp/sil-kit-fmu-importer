// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.Lin;

internal class LinController : ILinController
{
  public string ControllerName { get; set; }
  public string NetworkName { get; set; }

  private readonly Participant _participant;
  private readonly IntPtr _linControllerPtr;

  private LinFrameStatusHandler? _linFrameStatusHandler;
  private LinGoToSleepHandler? _linGoToSleepHandler;
  private LinWakeupHandler? _linWakeupHandler;

  internal LinController(Participant participant, string controllerName, string networkName)
  {
    _participant = participant;
    ControllerName = controllerName;
    NetworkName = networkName;

    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_Create(
        out _linControllerPtr,
        _participant.ParticipantPtr,
        controllerName,
        networkName));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_Create(
    [Out] out IntPtr outLinController,
    [In] IntPtr participant,
    [In, MarshalAs(UnmanagedType.LPStr)] string name,
    [In, MarshalAs(UnmanagedType.LPStr)] string network);

  public void Init(LinControllerConfig config)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_Init(
        _linControllerPtr,
        in config));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_Init(
    [In] IntPtr controller,
    [In] in LinControllerConfig config);

  public void SetFrameResponse(LinFrameResponse response)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_SetFrameResponse(
        _linControllerPtr,
        in response));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_SetFrameResponse(
    [In] IntPtr controller,
    [In] in LinFrameResponse response);

  public LinControllerStatus Status()
  {
    var statusPtr = Marshal.AllocHGlobal(sizeof(UInt32));
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LinController_Status(
          _linControllerPtr,
          statusPtr));

      return (LinControllerStatus)(uint)Marshal.ReadInt32(statusPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(statusPtr);
    }
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_Status(
    [In] IntPtr controller,
    [In] IntPtr outStatus);

  public UInt64 AddFrameStatusHandler(IntPtr context, LinFrameStatusHandler handler)
  {
    _linFrameStatusHandler = handler;

    var outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LinController_AddFrameStatusHandler(
          _linControllerPtr,
          context,
          _linFrameStatusHandler,
          outHandlerIdPtr));

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_AddFrameStatusHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] LinFrameStatusHandler handler,
    [In] IntPtr outHandlerId);

  public UInt64 AddGoToSleepHandler(IntPtr context, LinGoToSleepHandler handler)
  {
    _linGoToSleepHandler = handler;

    var outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LinController_AddGoToSleepHandler(
          _linControllerPtr,
          context,
          _linGoToSleepHandler,
          outHandlerIdPtr));

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_AddGoToSleepHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] LinGoToSleepHandler handler,
    [In] IntPtr outHandlerId);

  public UInt64 AddWakeupHandler(IntPtr context, LinWakeupHandler handler)
  {
    _linWakeupHandler = handler;

    var outHandlerIdPtr = Marshal.AllocHGlobal(sizeof(UInt64));
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_LinController_AddWakeupHandler(
          _linControllerPtr,
          context,
          _linWakeupHandler,
          outHandlerIdPtr));

      return (UInt64)Marshal.ReadInt64(outHandlerIdPtr);
    }
    finally
    {
      Marshal.FreeHGlobal(outHandlerIdPtr);
    }
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_AddWakeupHandler(
    [In] IntPtr controller,
    [In] IntPtr context,
    [In] LinWakeupHandler handler,
    [In] IntPtr outHandlerId);

  public void SendFrame(LinFrame frame, LinFrameResponseType responseType)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_SendFrame(
        _linControllerPtr,
        in frame,
        responseType));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_SendFrame(
    [In] IntPtr controller,
    [In] in LinFrame frame,
    [In] LinFrameResponseType responseType);

  public void SendFrameHeader(byte linId)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_SendFrameHeader(
        _linControllerPtr,
        linId));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_SendFrameHeader(
    [In] IntPtr controller,
    [In] byte linId);

  public void UpdateTxBuffer(LinFrame frame)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_UpdateTxBuffer(
        _linControllerPtr,
        in frame));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_UpdateTxBuffer(
    [In] IntPtr controller,
    [In] in LinFrame frame);

  public void GoToSleep()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_GoToSleep(_linControllerPtr));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_GoToSleep([In] IntPtr controller);

  public void GoToSleepInternal()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_GoToSleepInternal(_linControllerPtr));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_GoToSleepInternal([In] IntPtr controller);

  public void Wakeup()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_Wakeup(_linControllerPtr));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_Wakeup([In] IntPtr controller);

  public void WakeupInternal()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_LinController_WakeupInternal(_linControllerPtr));
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_LinController_WakeupInternal([In] IntPtr controller);
}
