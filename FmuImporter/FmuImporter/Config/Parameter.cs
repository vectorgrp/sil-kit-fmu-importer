// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Config;

public class ParameterValue
{
  public ParameterValue(object value)
  {
    Value = value;
  }

  public object Value { get; set; }
}

public class Parameter
{
  [Required(ErrorMessage = "VariableName is a required parameter attribute.")]
  public string VariableName { get; set; } = default!;

  [Required(ErrorMessage = "Value is a required parameter attribute.")]
  public ParameterValue Value { get; set; } = default!;
}
