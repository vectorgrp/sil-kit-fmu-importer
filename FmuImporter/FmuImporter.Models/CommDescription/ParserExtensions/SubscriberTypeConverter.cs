﻿// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FmuImporter.Models.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace FmuImporter.Models.CommDescription.ParserExtensions;

internal class SubscriberTypeConverter : IYamlTypeConverter
{
  public bool Accepts(Type type)
  {
    return type == typeof(List<SubscriberInternal>);
  }

  public object? ReadYaml(IParser parser, Type type)
  {
    if (!parser.TryConsume<SequenceStart>(out _))
    {
      throw new InvalidCommunicationInterfaceException(
        "Subscriber must be a list/sequence of subscriber entries. Expected entry format: <SubscriberName> : <TypeName>");
    }

    var subscriberList = ProcessSequence(parser);

    Validator.ValidateObject(subscriberList, new ValidationContext(subscriberList), true);

    return subscriberList;
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type)
  {
    throw new NotSupportedException();
  }

  private List<SubscriberInternal> ProcessSequence(IParser parser)
  {
    var subscriberList = new List<SubscriberInternal>();

    while (!parser.Accept<SequenceEnd>(out _))
    {
      var subscriber = new SubscriberInternal();
      if (!parser.TryConsume<MappingStart>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Subscriber entry not formatted as mapping. Expected format: <SubscriberName> : <TypeName>");
      }

      var success = parser.TryConsume<Scalar>(out var subscriberName);
      if (!success || string.IsNullOrEmpty(subscriberName?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Subscriber entry not formatted as mapping. Expected format: <SubscriberName> : <TypeName>");
      }

      success = parser.TryConsume<Scalar>(out var subscriberType);
      if (!success || string.IsNullOrEmpty(subscriberType?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Subscriber entry not formatted as mapping. Expected format: <SubscriberName> : <TypeName>");
      }

      if (!parser.TryConsume<MappingEnd>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Subscriber entry not formatted as mapping. Expected format: <SubscriberName> : <TypeName>");
      }

      subscriber.Name = subscriberName.Value;
      subscriber.Type = subscriberType.Value;

      Validator.ValidateObject(subscriber, new ValidationContext(subscriber), true);

      subscriberList.Add(subscriber);
    }

    parser.MoveNext();
    return subscriberList;
  }
}
