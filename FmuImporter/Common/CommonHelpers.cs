// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Binding;
using System.Text;
namespace Common;

internal static class Helpers
{
  // ---- Domain specific implementations ----
  public static Tuple<bool, StringBuilder?> ProcessFmiReturnCode(
    HashSet<int> okResultCodes,
    int resultCode,
    string returnCodeName,
    RuntimeMethodHandle? methodHandle,
    bool statusIsDiscardAndError)
  {
    // OK / Pending etc.
    if (okResultCodes.Contains(resultCode))
    {
      return new Tuple<bool, StringBuilder?>(true, null);
    }

    // FMI semantics: 1=Warning, 2=Discard (non-error unless explicitly treated as error)
    if (((resultCode == (int)FmiStatus.Warning) || (resultCode == (int)FmiStatus.Discard && !statusIsDiscardAndError)) && methodHandle != null)
    {
      var methodInfo = System.Reflection.MethodBase.GetMethodFromHandle(methodHandle.Value);
      if (methodInfo != null)
      {
        var sb = new StringBuilder(
          $"FMU Importer encountered an error with code '{resultCode}' ({returnCodeName})");
        var fullName = methodInfo.DeclaringType?.FullName + "." + methodInfo.Name;

        if (!string.IsNullOrEmpty(fullName))
        {
          sb.AppendLine($" while calling '{fullName}'.");
        }
        else
        {
          sb.AppendLine(".");
        }
        return new Tuple<bool, StringBuilder?>(true, sb);
      }
      return new Tuple<bool, StringBuilder?>(true, null);
    }

    return BuildErrorTuple(resultCode, returnCodeName, methodHandle);
  }

  public static Tuple<bool, StringBuilder?> ProcessSilKitReturnCode(
    HashSet<int> okResultCodes,
    int resultCode,
    string returnCodeName,
    RuntimeMethodHandle? methodHandle)
  {
    if (okResultCodes.Contains(resultCode))
    {
      return new Tuple<bool, StringBuilder?>(true, null);
    }
    return BuildErrorTuple(resultCode, returnCodeName, methodHandle);
  }

  private static Tuple<bool, StringBuilder?> BuildErrorTuple(
    int resultCode,
    string returnCodeName,
    RuntimeMethodHandle? methodHandle)
  {
    var errorMessageBuilder = new StringBuilder();
    errorMessageBuilder.Append(
      $"FMU Importer encountered an error with code '{resultCode}' ({returnCodeName})");

    if (methodHandle == null)
    {
      errorMessageBuilder.Append(
        ". Failed to identify name of method that caused the error.");
    }
    else
    {
      var methodInfo = System.Reflection.MethodBase.GetMethodFromHandle(methodHandle.Value);
      if (methodInfo == null)
      {
        errorMessageBuilder.Append(
          ". Failed to identify name of method that caused the error.");
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
