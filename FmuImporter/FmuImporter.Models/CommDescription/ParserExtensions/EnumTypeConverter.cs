// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FmuImporter.Models.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace FmuImporter.Models.CommDescription.ParserExtensions;

internal class EnumTypeConverter : IYamlTypeConverter
{
  public bool Accepts(Type type)
  {
    return type == typeof(List<EnumItem>);
  }

  public object? ReadYaml(IParser parser, Type type)
  {
    if (!parser.TryConsume<SequenceStart>(out _))
    {
      throw new InvalidCommunicationInterfaceException(
        "StructureDefinitions must be a list/sequence of structure definition entries.");
    }

    var enumItemList = ProcessSequence(parser);

    Validator.ValidateObject(enumItemList, new ValidationContext(enumItemList), true);

    return enumItemList;
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type)
  {
    throw new NotSupportedException();
  }

  private List<EnumItem> ProcessSequence(IParser parser)
  {
    var enumItemList = new List<EnumItem>();

    while (!parser.Accept<SequenceEnd>(out _))
    {
      var enumItem = new EnumItem();
      if (!parser.TryConsume<MappingStart>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Enum definition's item list entry not formatted as mapping. Expected format: <ItemName> : <Value>");
      }

      var success = parser.TryConsume<Scalar>(out var enumItemName);
      if (!success || string.IsNullOrEmpty(enumItemName?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Enum definition's item list entry not formatted as mapping. Expected format: <ItemName> : <Value>");
      }

      success = parser.TryConsume<Scalar>(out var enumItemValue);
      if (!success || string.IsNullOrEmpty(enumItemValue?.Value))
      {
        throw new InvalidCommunicationInterfaceException(
          "Enum definition's item list entry not formatted as mapping. Expected format: <ItemName> : <Value>");
      }

      success = Int64.TryParse(enumItemValue.Value, out var enumItemValueCasted);
      if (!success)
      {
        throw new InvalidCommunicationInterfaceException(
          "Enum definition's item list entry not formatted as mapping. Expected format: <ItemName> : <Value>");
      }

      if (!parser.TryConsume<MappingEnd>(out _))
      {
        throw new InvalidCommunicationInterfaceException(
          "Enum definition's item list entry '{enumItemName.Value}' did not have a valid value assigned to.");
      }

      enumItem.Name = enumItemName.Value;
      enumItem.Value = enumItemValueCasted;

      Validator.ValidateObject(enumItem, new ValidationContext(enumItem), true);

      enumItemList.Add(enumItem);
    }

    parser.MoveNext();
    return enumItemList;
  }
}
