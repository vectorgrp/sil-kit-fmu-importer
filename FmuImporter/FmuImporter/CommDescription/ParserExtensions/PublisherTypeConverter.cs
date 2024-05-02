// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;
using FmuImporter.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace FmuImporter.CommDescription.ParserExtensions;

public class PublisherTypeConverter : IYamlTypeConverter
{
  public bool Accepts(Type type)
  {
    return type == typeof(List<Publisher>);
  }

  public object? ReadYaml(IParser parser, Type type)
  {
    if (!parser.TryConsume<SequenceStart>(out _))
    {
      throw new InvalidCommunicationInterfaceException(
        "Publisher must be a list/sequence of publisher entries. Expected entry format: <PublisherName> : <TypeName>");
    }

    var publisherList = ProcessSequence(parser);

    Validator.ValidateObject(publisherList, new ValidationContext(publisherList), true);

    return publisherList;
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type)
  {
    throw new NotSupportedException();
  }

  private List<Publisher> ProcessSequence(IParser parser)
  {
    var publisherList = new List<Publisher>();

    while (!parser.Accept<SequenceEnd>(out _))
    {
      var publisher = new Publisher();
      if (!parser.TryConsume<MappingStart>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Publisher entry not formatted as mapping. Expected format: <PublisherName> : <TypeName>");
      }

      var success = parser.TryConsume<Scalar>(out var publisherName);
      if (!success || string.IsNullOrEmpty(publisherName?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Publisher entry not formatted as mapping. Expected format: <PublisherName> : <TypeName>");
      }

      success = parser.TryConsume<Scalar>(out var publisherType);
      if (!success || string.IsNullOrEmpty(publisherType?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Publisher entry not formatted as mapping. Expected format: <PublisherName> : <TypeName>");
      }

      if (!parser.TryConsume<MappingEnd>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Publisher entry not formatted as mapping. Expected format: <PublisherName> : <TypeName>");
      }

      publisher.Name = publisherName.Value;
      publisher.Type = publisherType.Value;

      Validator.ValidateObject(publisher, new ValidationContext(publisher), true);

      publisherList.Add(publisher);
    }

    parser.MoveNext();
    return publisherList;
  }
}
