using System.Runtime.InteropServices;
using static SilKit.SilKitVersion;

namespace SilKit.Services.PubSub
{
  public delegate void DataMessageHandler(
    IntPtr context,
    IDataSubscriber subscriber,
    DataMessageEvent dataMessageEvent);

  public struct DataMessageEvent
  {
    internal DataMessageEvent(DataMessageEventInternal internalDataMessageEvent)
    {
      this.timestampInNs = internalDataMessageEvent.timestampInNs;
      this.data = new byte[(int)internalDataMessageEvent.data.size];
      Marshal.Copy(internalDataMessageEvent.data.data, data, 0, data.Length);
    }

    public UInt64 timestampInNs;
    public byte[] data;
  }

  // Internal data types
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal struct DataMessageEventInternal
  {
    public DataMessageEventInternal()
    {
      structHeader = GetStructHeader(ServiceId.Data, DatatypeId.DataMessageEvent);
      timestampInNs = 0;
      data = new SilKit_ByteVector();
    }

    internal SilKit_StructHeader structHeader;
    public UInt64 timestampInNs;
    public SilKit_ByteVector data;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal struct SilKit_ByteVector
  {
    public IntPtr data;
    public IntPtr /*size_t*/ size;
  }
}
