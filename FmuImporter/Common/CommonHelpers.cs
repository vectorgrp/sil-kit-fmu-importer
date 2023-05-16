using System.Text;

namespace Common;

internal static class Helpers
{
  public static Tuple<bool, StringBuilder?> ProcessReturnCode(
    int resultCode,
    string returnCodeName,
    RuntimeMethodHandle? methodHandle)
  {
    if (resultCode is 0 or 5)
    {
      // 0 = OK
      // 5 = asynchronous doStep (FMI 2)
      return new Tuple<bool, StringBuilder?>(true, null);
    }

    if (methodHandle == null)
    {
      throw new NullReferenceException(
        $"Failed to identify name of method that caused error in native code " +
        $"with return code '{resultCode}' ({returnCodeName}).");
    }

    var methodInfo = System.Reflection.MethodBase.GetMethodFromHandle(methodHandle.Value);
    if (methodInfo == null)
    {
      throw new NullReferenceException(
        $"Failed to identify name of method that caused error in native code " +
        $"with return code '{resultCode}' ({returnCodeName}).");
    }

    var fullName = methodInfo.DeclaringType?.FullName + "." + methodInfo.Name;

    var errorMessageBuilder = new StringBuilder();
    errorMessageBuilder.Append(
      $"FMU Importer encountered an error with code '{resultCode}' ({returnCodeName})");
    if (!string.IsNullOrEmpty(fullName))
    {
      errorMessageBuilder.AppendLine($" while calling '{fullName}'.");
    }
    else
    {
      errorMessageBuilder.AppendLine(".");
    }

    return new Tuple<bool, StringBuilder?>(false, errorMessageBuilder);
  }
}
