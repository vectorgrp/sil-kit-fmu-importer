// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Runtime.InteropServices;

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
    var (success, errorBuilder) = Common.Helpers.ProcessSilKitReturnCode(
      sOkCodes,
      (int)statusCode,
      statusCode.ToString(),
      methodHandle);

    if (success)
    {
      return; // success
    }

    var sb = errorBuilder!;
    var nativeMsg = SilKit_GetLastErrorString();
    if (!string.IsNullOrWhiteSpace(nativeMsg))
    {
      sb.AppendLine("Provided error message: " + nativeMsg);
    }

    throw new SilKitReturnCodeException(statusCode, sb.ToString());
  }

  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern string SilKit_GetLastErrorString();
}
