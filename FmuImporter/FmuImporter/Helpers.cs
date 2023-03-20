namespace FmuImporter;

public static class Helpers
{
  /// <summary>
  /// Converts SIL Kit time (measured in nanoseconds) to FMI time (measured in seconds)
  /// </summary>
  /// <param name="silKitTimeInNs">The time used in SIL Kit (ns as ulong)</param>
  /// <returns>The time used in FMI (s as double)</returns>
  public static double SilKitTimeToFmiTime(ulong silKitTimeInNs)
  {
    return Convert.ToDouble(silKitTimeInNs / 1e9);
  }

  /// <summary>
  /// Converts FMI time (measured in seconds) to SIL Kit time (measured in nanoseconds)
  /// </summary>
  /// <param name="fmiTimeInS">The time used in FMI (s as double)</param>
  /// <returns>The time used in SIL Kit (ns as ulong)</returns>
  public static ulong FmiTimeToSilKitTime(double fmiTimeInS)
  {
    return Convert.ToUInt64(fmiTimeInS * 1e9);
  }

  public const ulong DefaultSimStepDuration = 1000000 /* 1ms */;
}
