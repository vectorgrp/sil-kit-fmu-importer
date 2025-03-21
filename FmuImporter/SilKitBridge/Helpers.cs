// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;
using SilKit.Services.Logger;

namespace SilKit;

internal static class Helpers
{
  public enum SilKit_ReturnCodes
  {
    SilKit_ReturnCode_SUCCESS = 0,
    SilKit_ReturnCode_UNSPECIFIEDERROR = 1,
    SilKit_ReturnCode_NOTSUPPORTED = 2,
    SilKit_ReturnCode_NOTIMPLEMENTED = 3,
    SilKit_ReturnCode_BADPARAMETER = 4,
    SilKit_ReturnCode_BUFFERTOOSMALL = 5,
    SilKit_ReturnCode_TIMEOUT = 6,
    SilKit_ReturnCode_UNSUPPORTEDSERVICE = 7,
    SilKit_ReturnCode_WRONGSTATE = 8 // Returned on exception SilKit::StateError (CapiImpl.h)
  }

  private static readonly HashSet<int> sOkCodes = new HashSet<int>
  {
    (int)SilKit_ReturnCodes.SilKit_ReturnCode_SUCCESS
  };

  public static void ProcessReturnCode(SilKit_ReturnCodes statusCode, RuntimeMethodHandle? methodHandle)
  {
    var result = Common.Helpers.ProcessReturnCode(
      sOkCodes,
      (int)statusCode,
      statusCode.ToString(),
      methodHandle,
      false);
    if (!result.Item1)
    {
      var sb = result.Item2!;
      var errorMessage = SilKit_GetLastErrorString();
      sb.AppendLine("Provided error message: " + errorMessage);

      try
      {
        throw new ApplicationException(sb.ToString());
      }
      catch (Exception e)
      {
        if (Participant.Logger != null)
        {
          Participant.Logger.Log(LogLevel.Error, e.Message);
          Participant.Logger.Log(LogLevel.Debug, e.ToString());
        }
        else
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine(
            $"Encountered exception: {e.Message}.\nMore information was written to the debug console.");
          Debug.WriteLine($"Encountered exception: {e}.");
          Console.ResetColor();
        }

        throw;
      }
    }
  }

  /*SilKitAPI const char* SilKitCALL SilKit_GetLastErrorString();*/
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern string SilKit_GetLastErrorString();
}
