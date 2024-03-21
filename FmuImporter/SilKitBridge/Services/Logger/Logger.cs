// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SilKit.Services.Logger;

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

public class Logger : ILogger
{
  private static IntPtr sLoggerPtr = IntPtr.Zero;

  internal IntPtr LoggerPtr
  {
    get
    {
      return sLoggerPtr;
    }
  }

  internal Logger(IntPtr participantPtr)
  {
    if (sLoggerPtr == IntPtr.Zero)
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_Participant_GetLogger(out sLoggerPtr, participantPtr),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
  }

  public void Log(LogLevel level, string message)
  {
    try
    {
      Helpers.ProcessReturnCode(
        (Helpers.SilKit_ReturnCodes)SilKit_Logger_Log(LoggerPtr, (UInt32)level, message),
        System.Reflection.MethodBase.GetCurrentMethod()?.MethodHandle);
    }
    catch (Exception e)
    {
      if (Environment.ExitCode == ExitCodes.Success)
      {
        Environment.ExitCode = ExitCodes.ErrorDuringLog;
      }

      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine("Logging via SIL Kit logger failed.");
      Console.WriteLine(
        $"Encountered exception: {e.Message}.\nMore information was written to the debug console.");
      Debug.WriteLine("Logging via SIL Kit logger failed.");
      Debug.WriteLine($"Encountered exception: {e}.");
      Console.ResetColor();
    }
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
