// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.CommDescription;

public class CommunicationInterface
{
  [Required]
  public int Version { get; set; } = default!;

  public List<EnumDefinition>? EnumDefinitions { get; set; }
  public List<StructDefinition>? StructDefinitions { get; set; }
  public List<Publisher>? Publishers { get; set; }
  public List<Subscriber>? Subscribers { get; set; }
  public string? InstanceName { get; set; }
  public string? Namespace { get; set; }
}
