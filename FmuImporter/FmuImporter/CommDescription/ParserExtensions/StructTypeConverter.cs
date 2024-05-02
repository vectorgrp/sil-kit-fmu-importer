// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;
using FmuImporter.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace FmuImporter.CommDescription.ParserExtensions;

public class StructTypeConverter : IYamlTypeConverter
{
  public bool Accepts(Type type)
  {
    return type == typeof(List<StructMember>);
  }

  public object? ReadYaml(IParser parser, Type type)
  {
    if (!parser.TryConsume<SequenceStart>(out _))
    {
      throw new InvalidCommunicationInterfaceException(
        "StructureDefinitions must be a list/sequence of structure definition entries.");
    }

    var structMemberList = ProcessSequence(parser);

    Validator.ValidateObject(structMemberList, new ValidationContext(structMemberList), true);

    return structMemberList;
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type)
  {
    throw new NotSupportedException();
  }

  private List<StructMember> ProcessSequence(IParser parser)
  {
    var structMemberList = new List<StructMember>();

    while (!parser.Accept<SequenceEnd>(out _))
    {
      var structMember = new StructMember();
      if (!parser.TryConsume<MappingStart>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Structure definition member entry not formatted as mapping. Expected format: <MemberName> : <TypeName>");
      }

      var success = parser.TryConsume<Scalar>(out var structMemberName);
      if (!success || string.IsNullOrEmpty(structMemberName?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Structure definition member entry not formatted as mapping. Expected format: <MemberName> : <TypeName>");
      }

      success = parser.TryConsume<Scalar>(out var structMemberType);
      if (!success || string.IsNullOrEmpty(structMemberType?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Structure definition member entry not formatted as mapping. Expected format: <MemberName> : <TypeName>");
      }

      if (!parser.TryConsume<MappingEnd>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Structure definition member entry not formatted as mapping. Expected format: <MemberName> : <TypeName>");
      }

      structMember.Name = structMemberName.Value;
      structMember.Type = structMemberType.Value;

      Validator.ValidateObject(structMember, new ValidationContext(structMember), true);

      structMemberList.Add(structMember);
    }

    parser.MoveNext();
    return structMemberList;
  }
}
