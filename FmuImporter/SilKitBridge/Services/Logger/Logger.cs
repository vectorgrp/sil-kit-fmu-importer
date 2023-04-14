using System.Runtime.InteropServices;

namespace SilKit.Services.Logger
{
  public enum LogLevel : UInt32
  {
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Critical,
    Off = 0xffffffff
  }
  public interface ILogger
  {
    public void Log(LogLevel level, string message);
  }
  public class Logger : ILogger
  {
    private static IntPtr loggerPtr = IntPtr.Zero;
    internal IntPtr LoggerPtr
    {
      get { return loggerPtr; }
    }

    internal Logger(IntPtr participantPtr)
    {
      if (loggerPtr == IntPtr.Zero)
      {
        Helpers.ProcessReturnCode(
          (Helpers.SilKit_ReturnCodes)SilKit_Participant_GetLogger(out loggerPtr, participantPtr),
          System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
      }
    }

    public void Log(LogLevel level, string message)
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_Logger_Log(LoggerPtr, (UInt32)level, message),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }

    /*
        SilKit_Logger_Log(SilKit_Logger* logger, SilKit_LoggingLevel level,
                          const char* message);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_Logger_Log(
      [In] IntPtr logger,
      [In] UInt32 level,
      [MarshalAs(UnmanagedType.LPStr)] string message);

    /*
      SilKit_Participant_GetLogger(SilKit_Logger** outLogger, SilKit_Participant* participant);
    */
    [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SilKit_Participant_GetLogger(
      out IntPtr outLogger,
      [In] IntPtr participant);
  }
}
