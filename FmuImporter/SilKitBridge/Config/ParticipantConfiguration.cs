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
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_ParticipantConfiguration_FromString(out configurationPtr, configurationString),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }

    /*
        SilKit_ParticipantConfiguration_FromString(
            SilKit_ParticipantConfiguration** outParticipantConfiguration,
            const char* participantConfigurationString);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_ParticipantConfiguration_FromString(
        [Out] out IntPtr outParticipantConfiguration,
        [MarshalAs(UnmanagedType.LPStr)] string participantConfigurationString);


    ~ParticipantConfiguration()
    {
      // TODO change to dispose pattern
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_ParticipantConfiguration_Destroy(ParticipantConfigurationPtr),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
      ParticipantConfigurationPtr = IntPtr.Zero;
    }

    /*
        SilKit_ParticipantConfiguration_Destroy(SilKit_ParticipantConfiguration* participantConfiguration);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_ParticipantConfiguration_Destroy([In] IntPtr participantConfiguration);

  }

}
