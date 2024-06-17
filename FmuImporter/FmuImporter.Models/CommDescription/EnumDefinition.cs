// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Models.CommDescription;

public class EnumDefinition
{
  [Required]
  public string Name { get; set; } = default!;

  public string? IndexType { get; set; }

  [Required, MinLength(1)]
  public List<EnumItem> Items { get; set; } = default!;
}
