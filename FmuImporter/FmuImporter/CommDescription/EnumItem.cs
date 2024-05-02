// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.CommDescription;

public class EnumItem
{
  [Required]
  public string Name { get; set; } = default!;

  [Required]
  public long? Value { get; set; }
}
