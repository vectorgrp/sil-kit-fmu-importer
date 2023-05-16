using System.Runtime.InteropServices;
using SilKit.Config;
using SilKit.Services.Logger;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;

namespace SilKit;

public class Participant : IDisposable
{
  private static readonly object lockObject = new object();
  internal static ILogger? Logger { get; private set; }

  private IntPtr participantPtr = IntPtr.Zero;

  internal IntPtr ParticipantPtr
  {
    get
    {
      return participantPtr;
    }
    private set
    {
      participantPtr = value;
    }
  }

#region ctor & dtor

  public Participant(ParticipantConfiguration configuration, string participantName, string registryUri)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_Participant_Create(
        out participantPtr,
        configuration.ParticipantConfigurationPtr,
        participantName,
        registryUri),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    Logger = new Logger(ParticipantPtr);
  }

  /*
      SilKit_Participant_Create(
          SilKit_Participant** outParticipant,
          SilKit_ParticipantConfiguration* participantConfiguration,
          const char* participantName, 
          const char* registryUri);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_Participant_Create(
    [Out] out IntPtr outParticipant,
    [In] IntPtr participantConfiguration,
    [MarshalAs(UnmanagedType.LPStr)] string participantName,
    [MarshalAs(UnmanagedType.LPStr)] string registryUri);


  ~Participant()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    SilKit_Participant_Destroy(ParticipantPtr);
    ParticipantPtr = IntPtr.Zero;
  }

  private bool mDisposedValue;

  protected void Dispose(bool disposing)
  {
    if (!mDisposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
      }

      ReleaseUnmanagedResources();
      mDisposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /*
   * SilKit_Participant_Destroy(SilKit_Participant* participant);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_Participant_Destroy([In] IntPtr participant);

#endregion ctor & dtor

  public ILifecycleService CreateLifecycleService(LifecycleService.LifecycleConfiguration lc)
  {
    return new LifecycleService(this, lc);
  }

  public IDataPublisher CreateDataPublisher(string controllerName, PubSubSpec dataSpec, byte history)
  {
    return new DataPublisher(this, controllerName, dataSpec, history);
  }

  public IDataSubscriber CreateDataSubscriber(
    string controllerName,
    PubSubSpec dataSpec,
    IntPtr context,
    DataMessageHandler dataMessageHandler)
  {
    return new DataSubscriber(this, controllerName, dataSpec, context, dataMessageHandler);
  }

  public ILogger GetLogger()
  {
    if (Logger == null)
    {
      throw new NullReferenceException("GetLogger failed to retrieve a logger object");
    }

    return Logger;
  }
}
