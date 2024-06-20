// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FmuImporter.Models.Helpers;

namespace FmuImporter.Models.CommDescription;

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
  public List<PublisherInternal>? Publishers { get; set; }

  [NullOrNotEmpty]
  public List<SubscriberInternal>? Subscribers { get; set; }

  public string? PublisherInstance { get; set; }
  public string? SubscriberInstance { get; set; }
  public string? Namespace { get; set; }
}
