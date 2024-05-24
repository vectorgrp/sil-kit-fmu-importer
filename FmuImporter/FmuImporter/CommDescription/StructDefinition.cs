// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.CommDescription;

public class StructDefinition
{
  [Required]
  public string Name { get; set; } = default!;

  [Required]
  public List<StructMemberInternal> Members { get; set; } = default!;
}
