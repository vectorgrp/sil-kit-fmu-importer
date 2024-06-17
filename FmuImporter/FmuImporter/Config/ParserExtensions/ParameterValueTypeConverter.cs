// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Models.Config;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace FmuImporter.Config.ParserExtensions;

internal class ParameterValueTypeConverter : IYamlTypeConverter
{
  public bool Accepts(Type type)
  {
    return type == typeof(ParameterValue);
  }

  public object ReadYaml(IParser parser, Type type)
  {
    if (parser.TryConsume<SequenceStart>(out _))
    {
      return StartParsingSequence(parser);
    }

    if (parser.TryConsume<Scalar>(out var scalar))
    {
      return new ParameterValue(ParseScalar(scalar));
    }

    throw new NotSupportedException("A parameter's \"Value\" attribute supports scalars or list of scalars only.");
  }

  private static object ParseScalar(Scalar scalar)
  {
    var value = scalar.Value;
    switch (scalar.Style)
    {
      case ScalarStyle.DoubleQuoted:
      case ScalarStyle.SingleQuoted:
        // All (double)quoted values are strings.
        break;
      default:
        if (UInt64.TryParse(value, out var actualValue))
        {
          return actualValue;
        }

        if (Int64.TryParse(value, out var actualSValue))
        {
          return actualSValue;
        }

        if (Double.TryParse(value, out var actualDValue))
        {
          return actualDValue;
        }

        // All other unquoted types and numerics that are not parseable (overflows etc) are handled as strings.
        break;
    }

    return value;
  }

  private static ParameterValue StartParsingSequence(IParser parser)
  {
    var resultList = new List<object>();
    while (!parser.Accept<SequenceEnd>(out _))
    {
      if (parser.TryConsume<Scalar>(out var scalar))
      {
        resultList.Add(ParseScalar(scalar));
      }
      else
      {
        throw new NotSupportedException("A parameter's \"Value\" attribute supports scalars or list of scalars only.");
      }
    }

    parser.MoveNext();
    return new ParameterValue(resultList);
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type)
  {
    throw new NotSupportedException();
  }
}
