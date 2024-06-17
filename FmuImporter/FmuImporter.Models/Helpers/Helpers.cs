// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace FmuImporter.Models.Helpers;

public static class Helpers
{
  public static OptionalType StringToType(string s)
  {
    var tg = new TypeGenerator(s);
    return tg.GenerateType();
  }

  private class TypeGenerator
  {
    private static readonly char[] delimiterChars = { '<', '>' };

    private readonly string _typeString;

    public TypeGenerator(string typeString)
    {
      _typeString = typeString;
    }

    public OptionalType GenerateType()
    {
      // Tokenize
      var tokens = _typeString.Split(delimiterChars, StringSplitOptions.TrimEntries).ToList();
      var type = TypeFromTokens(tokens);
      return type;
    }

    private OptionalType TypeFromTokens(List<string> typeStringTokens)
    {
      if (typeStringTokens.Count == 1)
      {
        // token must be plain type -> parse & return type
        return TypeFromToken(typeStringTokens[0]);
      }
      else
      {
        OptionalType type;
        if (!typeStringTokens[0].StartsWith("List"))
        {
          throw new ArgumentException(
            $"malformed type detected - contained unexpected token '{typeStringTokens[0]}'.",
            nameof(typeStringTokens));
        }

        // store entry after list type closed and remove first "List" entry...
        var lastEntry = typeStringTokens.Last();
        typeStringTokens = typeStringTokens.Skip(1).SkipLast(1).ToList();
        // Get result of type of list and declare type as array
        type = TypeFromTokens(typeStringTokens);
        var wrappedType = new OptionalType(lastEntry == "?", true, type);
        return wrappedType;
      }
    }

    private static string CanonizeTokenTypeName(string input)
    {
      switch (input.ToLowerInvariant())
      {
        case "bool" or "boolean":
          return "Boolean";
        case "sbyte" or "int8":
          return "SByte";
        case "short" or "int16":
          return "Int16";
        case "int" or "integer" or "int32":
          return "Int32";
        case "long" or "int64":
          return "Int64";
        case "byte" or "uint8":
          return "Byte";
        case "ushort" or "uint16":
          return "UInt16";
        case "uint" or "uint32":
          return "UInt32";
        case "ulong" or "uint64":
          return "UInt64";
        case "float" or "float32":
          return "Single";
        case "float64" or "real" or "double":
          return "Double";
        case "string":
          return "String";
        case "binary" or "byte[]":
          return "Byte[]";
      }

      return input;
    }

    /// <summary>
    ///   Converts the provided token to a data type and wraps it in an OptionalType
    ///   If the token is not the string representation of a built-in type,
    ///   the OptionalType will contain the type string in the CustomTypeName instead of providing an actual type,
    /// </summary>
    /// <param name="tokenString">
    ///   The string representation of a type.
    ///   May have a '?' as a suffix to indicate that it is nullable.
    /// </param>
    /// <returns>The parsed type representation as an OptionalType</returns>
    private OptionalType TypeFromToken(string tokenString)
    {
      var isOptional = tokenString[tokenString.Length - 1] == '?';

      var unaliasedTokenNoNullable = (isOptional)
                                       ? tokenString.Substring(0, tokenString.Length - 1)
                                       : tokenString;
      var resolvedTypeName = CanonizeTokenTypeName(unaliasedTokenNoNullable.ToLowerInvariant());

      try
      {
        var type = Type.GetType("System." + resolvedTypeName);
        if (type == null)
        {
          // NB: Structs and enums cannot be resolved at this point - return their name instead of an actual type
          return new OptionalType(isOptional, false, unaliasedTokenNoNullable);
        }
        else
        {
          return new OptionalType(isOptional, false, type);
        }
      }
      catch
      {
        throw;
      }
    }
  }
}
