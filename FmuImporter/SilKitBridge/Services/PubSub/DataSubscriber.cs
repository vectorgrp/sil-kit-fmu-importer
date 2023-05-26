// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Services.PubSub;

public interface IDataSubscriber
{
  public void SetDataMessageHandler(DataMessageHandler dataMessageHandler);
}

public class DataSubscriber : IDataSubscriber
{
  private DataMessageHandler? _dataMessageHandler;
  private readonly SilKit_DataMessageHandler_t dataMessageHandlerDelegate;

  private readonly Participant participant;
  private readonly IntPtr datahandlerContext;
  private IntPtr dataSubscriberPtr;

  internal IntPtr DataSubscriberPtr
  {
    get
    {
      return dataSubscriberPtr;
    }
    private set
    {
      dataSubscriberPtr = value;
    }
  }

  internal DataSubscriber(
    Participant participant,
    string controllerName,
    PubSubSpec dataSpec,
    IntPtr dataHandlerContext,
    DataMessageHandler dataMessageHandler)
  {
    dataMessageHandlerDelegate = DataMessageHandlerInternal;

    this.participant = participant;
    datahandlerContext = dataHandlerContext;
    _dataMessageHandler = dataMessageHandler;
    var silKitDataSpec = dataSpec.toSilKitDataSpec();

    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_DataSubscriber_Create(
        out dataSubscriberPtr,
        participant.ParticipantPtr,
        controllerName,
        silKitDataSpec,
        dataHandlerContext,
        dataMessageHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }


  public void SetDataMessageHandler(DataMessageHandler dataMessageHandler)
  {
    _dataMessageHandler = dataMessageHandler;

    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_DataSubscriber_SetDataMessageHandler(
        DataSubscriberPtr,
        IntPtr.Zero,
        dataMessageHandlerDelegate),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

  private void DataMessageHandlerInternal(
    IntPtr context,
    IntPtr subscriber,
    ref DataMessageEventInternal dataMessageEvent)
  {
    // double check if this is the correct lifecycle service
    if (subscriber != DataSubscriberPtr)
    {
      return;
    }

    _dataMessageHandler?.Invoke(datahandlerContext, this, new DataMessageEvent(dataMessageEvent));
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
    [In] SilKit_DataMessageHandler_t dataHandler);

  private delegate void SilKit_DataMessageHandler_t(
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
    SilKit_DataMessageHandler_t dataHandler);
}
