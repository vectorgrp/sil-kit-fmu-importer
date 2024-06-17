// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Models.CommDescription;

public class StructMember
{
  [Required]
  public string Name { get; set; } = default!;

  [Required]
  public string Type { get; set; } = default!;
}
