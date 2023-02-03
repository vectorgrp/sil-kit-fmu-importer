using System.Runtime.InteropServices;

namespace SilKit.Services.PubSub
{
  public interface IDataSubscriber
  {
    public void SetDataMessageHandler(DataMessageHandler dataMessageHandler);
  }

  public class DataSubscriber : IDataSubscriber
  {
    private DataMessageHandler? dataMessageHandler;

    private readonly Participant participant;
    private IntPtr dataSubscriberPtr;
    internal IntPtr DataSubscriberPtr
    {
      get { return dataSubscriberPtr; }
      private set { dataSubscriberPtr = value; }
    }

    internal DataSubscriber(Participant participant, string controllerName, PubSubSpec dataSpec, object dataHandlerContext,
        DataMessageHandler dataMessageHandler)
    {
      this.participant = participant;
      this.dataMessageHandler = dataMessageHandler;
      var silKitDataSpec = dataSpec.toSilKitDataSpec();

      SilKit_DataSubscriber_Create(out dataSubscriberPtr, participant.ParticipantPtr, controllerName, silKitDataSpec,
          out _, DataMessageHandlerInternal);
    }


    public void SetDataMessageHandler(DataMessageHandler dataMessageHandler)
    {
      this.dataMessageHandler = dataMessageHandler;
      SilKit_DataSubscriber_SetDataMessageHandler(DataSubscriberPtr, IntPtr.Zero, DataMessageHandlerInternal);
    }
    private void DataMessageHandlerInternal(IntPtr context, IntPtr subscriber, ref DataMessageEventInternal dataMessageEvent)
    {
      // double check if this is the correct lifecycle service
      if (subscriber != DataSubscriberPtr) { return; }


      dataMessageHandler?.Invoke(new DataMessageEvent(dataMessageEvent));
    }

    /*
        SilKit_DataSubscriber_SetDataMessageHandler(
            SilKit_DataSubscriber* self, 
            void* context, 
            SilKit_DataMessageHandler_t dataHandler);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_DataSubscriber_SetDataMessageHandler(
        [In] IntPtr self,
        [In] IntPtr context,
        [In] SilKit_DataMessageHandler_t dataHandler);
    private delegate void SilKit_DataMessageHandler_t(IntPtr context, IntPtr subscriber, ref DataMessageEventInternal dataMessageEvent);


    /*
        SilKit_DataSubscriber_Create(
            SilKit_DataSubscriber** outSubscriber,
            SilKit_Participant* participant,
            const char* controllerName,
            SilKit_DataSpec* dataSpec, 
            void* dataHandlerContext,
            SilKit_DataMessageHandler_t dataHandler);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_DataSubscriber_Create(
        [Out] out IntPtr outSubscriber,
        [In] IntPtr participant,
        [MarshalAs(UnmanagedType.LPStr)] string controllerName,
        [In] SilKit_DataSpec dataSpec,
        [Out] out IntPtr context,
        SilKit_DataMessageHandler_t dataHandler);
  }
}
