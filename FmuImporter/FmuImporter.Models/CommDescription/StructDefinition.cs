// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Models.CommDescription;

public class StructDefinition
{
  [Required]
  public string Name { get; set; } = default!;

  [Required]
  public List<StructMemberInternal> Members { get; set; } = default!;
}
