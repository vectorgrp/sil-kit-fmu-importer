using System.Runtime.InteropServices;

namespace SilKit.Config
{
  public class ParticipantConfiguration
  {
    private IntPtr configurationPtr;
    internal IntPtr ParticipantConfigurationPtr
    {
      get { return configurationPtr; }
      private set { configurationPtr = value; }
    }
    internal ParticipantConfiguration(string configurationString)
    {
      SilKit_ParticipantConfiguration_FromString(out configurationPtr, configurationString);
    }

    /*
        SilKit_ParticipantConfiguration_FromString(
            SilKit_ParticipantConfiguration** outParticipantConfiguration,
            const char* participantConfigurationString);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_ParticipantConfiguration_FromString(
        [Out] out IntPtr outParticipantConfiguration,
        [MarshalAs(UnmanagedType.LPStr)] string participantConfigurationString);


    ~ParticipantConfiguration()
    {
      // TODO change to dispose pattern
      SilKit_ParticipantConfiguration_Destroy(ParticipantConfigurationPtr);
      ParticipantConfigurationPtr = IntPtr.Zero;
    }

    /*
        SilKit_ParticipantConfiguration_Destroy(SilKit_ParticipantConfiguration* participantConfiguration);
    */
    [DllImport("SilKitd.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_ParticipantConfiguration_Destroy([In] IntPtr participantConfiguration);

  }

}
