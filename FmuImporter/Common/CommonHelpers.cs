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
    string callerName,
    bool statusIsDiscardAndError)
  {
    // OK / Pending etc.
    if (okResultCodes.Contains(resultCode))
    {
      return new Tuple<bool, StringBuilder?>(true, null);
    }

    // FMI semantics: 1=Warning, 2=Discard (non-error unless explicitly treated as error)
    if (((resultCode == (int)FmiStatus.Warning) || (resultCode == (int)FmiStatus.Discard && !statusIsDiscardAndError)))
    {
      var sb = new StringBuilder(
        $"FMU Importer encountered a warning with code '{resultCode}' ({returnCodeName})");

      if (!string.IsNullOrEmpty(callerName))
      {
        sb.AppendLine($" while calling '{callerName}'.");
      }
      else
      {
        sb.AppendLine(".");
      }
      return new Tuple<bool, StringBuilder?>(true, sb);
    }

    return BuildErrorTuple(resultCode, returnCodeName, callerName);
  }

  public static Tuple<bool, StringBuilder?> ProcessSilKitReturnCode(
    HashSet<int> okResultCodes,
    int resultCode,
    string returnCodeName,
    string callerName)
  {
    if (okResultCodes.Contains(resultCode))
    {
      return new Tuple<bool, StringBuilder?>(true, null);
    }
    return BuildErrorTuple(resultCode, returnCodeName, callerName);
  }

  private static Tuple<bool, StringBuilder?> BuildErrorTuple(
    int resultCode,
    string returnCodeName,
    string callerName)
  {
    var errorMessageBuilder = new StringBuilder();
    errorMessageBuilder.Append(
      $"FMU Importer encountered an error with code '{resultCode}' ({returnCodeName})");

    if (string.IsNullOrEmpty(callerName))
    {
      errorMessageBuilder.Append(
        ". Failed to identify name of method that caused the error.");
    }
    else
    {
      errorMessageBuilder.AppendLine($" while calling '{callerName}'.");
    }

    return new Tuple<bool, StringBuilder?>(false, errorMessageBuilder);
  }
}
