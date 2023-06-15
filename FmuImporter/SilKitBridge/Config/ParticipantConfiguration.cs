// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

namespace SilKit.Config;

public class ParticipantConfiguration : IDisposable
{
  private IntPtr _configurationPtr;

  internal IntPtr ParticipantConfigurationPtr
  {
    get
    {
      return _configurationPtr;
    }
    private set
    {
      _configurationPtr = value;
    }
  }

  internal ParticipantConfiguration(string configurationString)
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_ParticipantConfiguration_FromString(
        out _configurationPtr,
        configurationString),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
  }

#region IDisposable

  ~ParticipantConfiguration()
  {
    Dispose(false);
  }

  private void ReleaseUnmanagedResources()
  {
    Helpers.ProcessReturnCode(
      (Helpers.SilKit_ReturnCodes)SilKit_ParticipantConfiguration_Destroy(ParticipantConfigurationPtr),
      System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    ParticipantConfigurationPtr = IntPtr.Zero;
  }

  private bool _disposedValue;

  protected void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // dispose managed objects
      }

      ReleaseUnmanagedResources();
      _disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

#endregion IDisposable

  /*
      SilKit_ParticipantConfiguration_FromString(
          SilKit_ParticipantConfiguration** outParticipantConfiguration,
          const char* participantConfigurationString);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_ParticipantConfiguration_FromString(
    [Out] out IntPtr outParticipantConfiguration,
    [MarshalAs(UnmanagedType.LPStr)] string participantConfigurationString);

  /*
      SilKit_ParticipantConfiguration_Destroy(SilKit_ParticipantConfiguration* participantConfiguration);
  */
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern int SilKit_ParticipantConfiguration_Destroy([In] IntPtr participantConfiguration);
}
