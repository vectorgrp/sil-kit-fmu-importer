// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;
using FmuImporter.Helpers;

namespace FmuImporter.CommDescription;

public class CommunicationInterface
{
  public const int MaxCommInterfaceVersion = 1;

  [Required(ErrorMessage = "Version is a required attribute."), Range(1, MaxCommInterfaceVersion)]
  public int Version { get; set; } = default!;

  [NullOrNotEmpty]
  public List<EnumDefinition>? EnumDefinitions { get; set; }

  [NullOrNotEmpty]
  public List<StructDefinitionInternal>? StructDefinitions { get; set; }

  [NullOrNotEmpty]
  public List<Publisher>? Publishers { get; set; }

  [NullOrNotEmpty]
  public List<Subscriber>? Subscribers { get; set; }

  public string? InstanceName { get; set; }
  public string? Namespace { get; set; }
}
