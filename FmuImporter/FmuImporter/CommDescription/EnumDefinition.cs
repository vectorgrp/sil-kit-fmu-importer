// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.CommDescription;

public class EnumDefinition
{
  [Required]
  public string Name { get; set; } = default!;

  public string? IndexType { get; set; }

  [Required]
  public List<EnumItem> Items { get; set; } = default!;
}
