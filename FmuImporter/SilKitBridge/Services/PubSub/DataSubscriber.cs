// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.PubSub;

public class DataSubscriber : IDataSubscriber
{
  private PubSub.DataMessageHandler? _dataMessageHandler;
  private readonly DataMessageHandler _dataMessageHandlerDelegate;

  private readonly Participant _participant;
  private readonly IntPtr _datahandlerContext;
  private IntPtr _dataSubscriberPtr;

  internal IntPtr DataSubscriberPtr
  {
    get
    {
      return _dataSubscriberPtr;
    }
    private set
    {
      _dataSubscriberPtr = value;
    }
  }

  internal DataSubscriber(
    Participant participant,
    string controllerName,
    PubSubSpec dataSpec,
    IntPtr dataHandlerContext,
    PubSub.DataMessageHandler dataMessageHandler)
  {
    _dataMessageHandlerDelegate = DataMessageHandlerInternal;

    _participant = participant;
    _datahandlerContext = dataHandlerContext;
    _dataMessageHandler = dataMessageHandler;
    var silKitDataSpec = dataSpec.ToSilKitDataSpec();

    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_DataSubscriber_Create(
        out _dataSubscriberPtr,
        _participant.ParticipantPtr,
        controllerName,
        silKitDataSpec,
        dataHandlerContext,
        _dataMessageHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }


  public void SetDataMessageHandler(PubSub.DataMessageHandler dataMessageHandler)
  {
    _dataMessageHandler = dataMessageHandler;

    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_DataSubscriber_SetDataMessageHandler(
        DataSubscriberPtr,
        IntPtr.Zero,
        _dataMessageHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  private void DataMessageHandlerInternal(
    IntPtr context,
    IntPtr subscriber,
    ref DataMessageEventInternal dataMessageEvent)
  {
    // double check if this is the correct service
    if (subscriber != DataSubscriberPtr)
    {
      return;
    }

    _dataMessageHandler?.Invoke(_datahandlerContext, this, new DataMessageEvent(dataMessageEvent));
  }

  /*
      SilKit_DataSubscriber_SetDataMessageHandler(
          SilKit_DataSubscriber* self, 
          void* context, 
          SilKit_DataMessageHandler_t dataHandler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_DataSubscriber_SetDataMessageHandler(
    [In] IntPtr self,
    [In] IntPtr context,
    [In] DataMessageHandler dataHandler);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void DataMessageHandler(
    IntPtr context,
    IntPtr subscriber,
    ref DataMessageEventInternal dataMessageEvent);


  /*
      SilKit_DataSubscriber_Create(
          SilKit_DataSubscriber** outSubscriber,
          SilKit_Participant* participant,
          const char* controllerName,
          SilKit_DataSpec* dataSpec, 
          void* dataHandlerContext,
          SilKit_DataMessageHandler_t dataHandler);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_DataSubscriber_Create(
    [Out] out IntPtr outSubscriber,
    [In] IntPtr participant,
    [MarshalAs(UnmanagedType.LPStr)] string controllerName,
    [In] IntPtr dataSpec,
    IntPtr context,
    DataMessageHandler dataHandler);
}
