// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;

namespace Fmi.Supplements;

public static class StructuredVariableParser
{
  public static StructuredNameContainer Parse(string variableName)
  {
    try
    {
      var result = new List<string>();

      if (string.IsNullOrEmpty(variableName))
      {
        throw new ParserException("variable names must not be empty.");
      }

      if (variableName[0] == '.')
      {
        throw new ParserException("variable names must not start with the separator character (.)");
      }

      Parse(variableName.AsSpan(), result);

      return new StructuredNameContainer(result);
    }
    catch (ParserException e)
    {
      throw new ParserException($"Encountered parser exception while parsing variable '{variableName}'.", e);
    }
  }

  private static void CheckIndexAndContinue(ReadOnlySpan<char> input, int lastProcessedIndex, List<string> path)
  {
    // lastProcessedIndex     = index of the last character of the processed substring
    // lastProcessedIndex + 1 = separator or end of input
    // lastProcessedIndex + 2 = any character other than another separator

    if (lastProcessedIndex == input.Length - 1)
    {
      // reached end of input
      return;
    }

    if (input[lastProcessedIndex + 1] != '.')
    {
      // Check for existence of separator character
      throw new ParserException($"Missing separator character detected.");
    }

    if (lastProcessedIndex + 1 == input.Length - 1)
    {
      // trailing separator character is forbidden
      throw new ParserException($"Trailing separator character detected.");
    }

    if (input[lastProcessedIndex + 2] == '.')
    {
      // leading separator character is forbidden
      throw new ParserException("Two consecutive separator characters detected.");
    }

    Parse(input.Slice(lastProcessedIndex + 2), path);
  }

  private static void Parse(ReadOnlySpan<char> input, List<string> path)
  {
    if (input.IsEmpty)
    {
      return;
    }

    // FMI supports any variable name (even with blanks, escapes, ...) as long as it is escaped via apostrophes
    var nextQuoteIndex = input.IndexOf("'");
    if (nextQuoteIndex == -1)
    {
      // no quoted variables - process input completely
      ProcessUnquotedStructuredVariable(input, path);
      return;
    }

    int nextParseIndex;

    if (nextQuoteIndex == 0)
    {
      // first character is an apostrophe -> find index of end of quoted name
      var endOfQuotedNameIndex = FindEndOfQuoteName(input.Slice(1));
      // add the quoted variable without apostrophes... 
      path.Add(input.Slice(1, endOfQuotedNameIndex).ToString());
      // ... and continue recursively with the remaining input
      // provide index of last quote as index
      nextParseIndex = endOfQuotedNameIndex + 1;
    }
    else
    {
      nextParseIndex = nextQuoteIndex - 2;
      ProcessUnquotedStructuredVariable(input.Slice(0, nextQuoteIndex - 1), path);
    }

    // process input up to the next quoted variable...
    // ... and continue recursively with the remaining input
    CheckIndexAndContinue(input, nextParseIndex, path);
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

  private static void ProcessUnquotedStructuredVariable(ReadOnlySpan<char> input, List<string> path)
  {
    var nextIndex = 0;
    // no more quoted variables - process input completely
    while (!input.IsEmpty && nextIndex != -1)
    {
      nextIndex = input.IndexOf('.');
      if (nextIndex == input.Length - 1)
      {
        throw new ParserException("Trailing separator detected.");
      }

      if (nextIndex == -1)
      {
        path.Add(input.ToString());
        continue;
      }

      if (nextIndex == 0)
      {
        throw new ParserException("Two consecutive separators detected.");
      }

      path.Add(input.Slice(0, nextIndex).ToString());
      input = input.Slice(nextIndex + 1);
    }
  }
}
