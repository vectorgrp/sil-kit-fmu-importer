using System.Runtime.InteropServices;
using SilKit.Config;
using SilKit.Services.Orchestration;
using SilKit.Services.PubSub;

namespace SilKit
{
  public class Participant
  {
    private readonly ParticipantConfiguration configuration;
    private IntPtr participantPtr;
    internal IntPtr ParticipantPtr
    {
      get { return participantPtr; }
      private set { participantPtr = value; }
    }

    #region ctor & dtor
    public Participant(ParticipantConfiguration configuration, string participantName, string registryUri)
    {
      this.configuration = configuration;
      SilKit_Participant_Create(out participantPtr, configuration.ParticipantConfigurationPtr, participantName, registryUri);
    }

    /*
        SilKit_Participant_Create(
            SilKit_Participant** outParticipant,
            SilKit_ParticipantConfiguration* participantConfiguration,
            const char* participantName, 
            const char* registryUri);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_Participant_Create(
        [Out] out IntPtr outParticipant,
        [In] IntPtr participantConfiguration,
        [MarshalAs(UnmanagedType.LPStr)] string participantName,
        [MarshalAs(UnmanagedType.LPStr)] string registryUri);


    ~Participant()
    {
      // TODO change to dispose pattern
      SilKit_Participant_Destroy(ParticipantPtr);
      ParticipantPtr = IntPtr.Zero;
    }

    /*
     * SilKit_Participant_Destroy(SilKit_Participant* participant);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
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

    public IDataSubscriber CreateDataSubscriber(string controllerName, PubSubSpec dataSpec, DataMessageHandler dataMessageHandler)
    {
      return new DataSubscriber(this, controllerName, dataSpec, IntPtr.Zero, dataMessageHandler);
    }
  }
}
