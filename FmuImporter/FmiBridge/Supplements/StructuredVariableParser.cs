// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;

namespace Fmi.Supplements;

public static class StructuredVariableParser
{
  public static List<string> Parse(string variableName)
  {
    var result = new List<string>();
    Parse(variableName.AsSpan(), ref result);
    return result;
  }

  private static void Parse(ReadOnlySpan<char> input, ref List<string> path)
  {
    if (input.IsEmpty)
    {
      return;
    }

    if (input[0] == '.')
    {
      // skip separator character
      Parse(input.Slice(1), ref path);
      return;
    }

    // FMI supports any variable name (even with blanks, escapes, ...) as long as it is escaped via apostrophes
    var nextIndex = input.IndexOf("'");
    if (nextIndex == -1)
    {
      // no quoted variables - process input completely
      ProcessUnquotedStructuredVariable(input, ref path);
      return;
    }

    if (nextIndex == 0)
    {
      // first character is an apostrophe -> find index of end of quoted name
      nextIndex = FindEndOfQuoteName(input.Slice(1));
      // add the quoted variable without apostrophes... 
      path.Add(input.Slice(1, nextIndex).ToString());
      // ... and continue recursively with the remaining input
      // skip both quotes
      Parse(input.Slice(nextIndex + 2), ref path);
      return;
    }

    // process input up to the next quoted variable...
    ProcessUnquotedStructuredVariable(input.Slice(0, nextIndex), ref path);
    // ... and continue recursively with the remaining input
    Parse(input.Slice(nextIndex), ref path);
  }

  private static int FindEndOfQuoteName(ReadOnlySpan<char> input)
  {
    var scanIndex = input.IndexOf('\'');
    if (scanIndex == -1)
    {
      throw new ParserException("The processed topic name is invalid.");
    }

    if (scanIndex == 0)
    {
      return 0;
    }

    if (input[scanIndex - 1] == '\\')
    {
      // quote was escaped - repeat search with remaining string
      return scanIndex + FindEndOfQuoteName(input.Slice(scanIndex + 1)) + 1;
    }

    return scanIndex;
  }

  private static void ProcessUnquotedStructuredVariable(ReadOnlySpan<char> input, ref List<string> path)
  {
    var nextIndex = 0;
    // no more quoted variables - process input completely
    while (!input.IsEmpty && nextIndex != -1)
    {
      nextIndex = input.IndexOf('.');
      if (nextIndex == -1)
      {
        path.Add(input.ToString());
      }
      else
      {
        path.Add(input.Slice(0, nextIndex).ToString());
        input = input.Slice(nextIndex + 1);
      }
    }
  }
}
