// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Config;

public class Parameter
{
  [Required(ErrorMessage = "VariableName is a required parameter attribute.")]
  public string VariableName { get; set; } = default!;

  [Required(ErrorMessage = "Value is a required parameter attribute."),
   StringLength(int.MaxValue, MinimumLength = 1, ErrorMessage = "A parameter value attribute must not be empty.")]
  public object Value { get; set; } = default!;
}
