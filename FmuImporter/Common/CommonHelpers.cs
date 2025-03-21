// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;

namespace Common;

internal static class Helpers
{
  public static Tuple<bool, StringBuilder?> ProcessReturnCode(
    HashSet<int> okResultCodes,
    int resultCode,
    string returnCodeName,
    RuntimeMethodHandle? methodHandle,
    bool statusIsDiscardAndError)
  {
    if (okResultCodes.Contains(resultCode))
    {
      return new Tuple<bool, StringBuilder?>(true, null);
    }

    if ((resultCode == 1 /* warning */ || (resultCode == 2 /* discard */ && !statusIsDiscardAndError)) && methodHandle != null)
    {
      var methodInfo = System.Reflection.MethodBase.GetMethodFromHandle(methodHandle.Value);
      if (methodInfo != null)
      {
        var stringBuilder = new StringBuilder(
          $"FMU Importer encountered a call with return value '{resultCode}' ({returnCodeName})");
        var fullName = methodInfo.DeclaringType?.FullName + "." + methodInfo.Name;

        if (!string.IsNullOrEmpty(fullName))
        {
          stringBuilder.AppendLine($" while calling '{fullName}'.");
        }
        else
        {
          stringBuilder.AppendLine(".");
        }

        return new Tuple<bool, StringBuilder?>(true, stringBuilder);
      }
      else
      {
        return new Tuple<bool, StringBuilder?>(true, null);
      }
    }

    var errorMessageBuilder = new StringBuilder();
    errorMessageBuilder.Append(
      $"FMU Importer encountered an error with code '{resultCode}' ({returnCodeName})");

    if (methodHandle == null)
    {
      errorMessageBuilder.Append(
        ". Failed to identify name of method that caused error in native code.");
    }
    else
    {
      var methodInfo = System.Reflection.MethodBase.GetMethodFromHandle(methodHandle.Value);
      if (methodInfo == null)
      {
        errorMessageBuilder.Append(
          ". Failed to identify name of method that caused error in native code.");
      }
      else
      {
        var fullName = methodInfo.DeclaringType?.FullName + "." + methodInfo.Name;

        if (!string.IsNullOrEmpty(fullName))
        {
          errorMessageBuilder.AppendLine($" while calling '{fullName}'.");
        }
        else
        {
          errorMessageBuilder.AppendLine(".");
        }
      }
    }

    return new Tuple<bool, StringBuilder?>(false, errorMessageBuilder);
  }
}
