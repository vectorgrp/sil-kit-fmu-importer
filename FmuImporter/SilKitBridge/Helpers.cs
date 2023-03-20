using System.Runtime.InteropServices;
using System.Text;

namespace SilKit;

internal static class Helpers
{
  public enum SilKit_ReturnCodes
  {
    SilKit_ReturnCode_SUCCESS            = 0,
    SilKit_ReturnCode_UNSPECIFIEDERROR   = 1,
    SilKit_ReturnCode_NOTSUPPORTED       = 2,
    SilKit_ReturnCode_NOTIMPLEMENTED     = 3,
    SilKit_ReturnCode_BADPARAMETER       = 4,
    SilKit_ReturnCode_BUFFERTOOSMALL     = 5,
    SilKit_ReturnCode_TIMEOUT            = 6,
    SilKit_ReturnCode_UNSUPPORTEDSERVICE = 7,
    SilKit_ReturnCode_WRONGSTATE         = 8 // Returned on exception SilKit::StateError (CapiImpl.h)
  }

  public static void ProcessReturnCode(SilKit_ReturnCodes statusCode, RuntimeMethodHandle? methodHandle)
  {
    var result = Common.Helpers.ProcessReturnCode(
      (int)statusCode,
      statusCode.ToString(),
      methodHandle);
    if (!result.Item1)
    {
      StringBuilder sb = result.Item2!;
      var errorMessage = SilKit_GetLastErrorString();
      sb.AppendLine($"Provided error message: " + errorMessage);

      try
      {
        throw new ApplicationException(sb.ToString());
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }
    }
  }

  /*SilKitAPI const char* SilKitCALL SilKit_GetLastErrorString();*/
  [DllImport("SilKit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  private static extern string SilKit_GetLastErrorString();
}
