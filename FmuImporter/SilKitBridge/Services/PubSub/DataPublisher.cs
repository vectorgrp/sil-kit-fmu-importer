using System.Runtime.InteropServices;

namespace SilKit.Services.PubSub
{
  public interface IDataPublisher
  {
    public void Publish(byte[] data);
  }

  public class DataPublisher : IDataPublisher
  {
    private readonly Participant participant;

    private IntPtr dataPublisherPtr;
    internal IntPtr DataPublisherPtr
    {
      get { return dataPublisherPtr; }
      private set { dataPublisherPtr = value; }
    }

    internal DataPublisher(Participant participant, string controllerName, PubSubSpec dataSpec, byte history)
    {
      this.participant = participant;
      var silKitDataSpec = dataSpec.toSilKitDataSpec();

      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_DataPublisher_Create(
          out dataPublisherPtr,
          participant.ParticipantPtr,
          controllerName,
          silKitDataSpec,
          history),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }

    /*
        SilKit_DataPublisher_Create(
            SilKit_DataPublisher** outPublisher,
            SilKit_Participant* participant,
            const char* controllerName,
            SilKit_DataSpec* dataSpec, 
            uint8_t history);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_DataPublisher_Create(
      [Out] out IntPtr outPublisher,
      [In] IntPtr participant,
      [In] [MarshalAs(UnmanagedType.LPStr)] string controllerName,
      [In] IntPtr dataSpec,
      [In] byte history);

    public void Publish(byte[] data)
    {
      GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
      var dataPtr = handle.AddrOfPinnedObject();
      var byteVector = new SilKit_ByteVector()
      {
        data = dataPtr,
        size = (IntPtr)data.Length
      };
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_DataPublisher_Publish(DataPublisherPtr, byteVector),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
      handle.Free();
    }

    /*
        SilKit_DataPublisher_Publish(
            SilKit_DataPublisher* self, 
            const SilKit_ByteVector* data);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_DataPublisher_Publish(IntPtr self, SilKit_ByteVector data);
  }
}
