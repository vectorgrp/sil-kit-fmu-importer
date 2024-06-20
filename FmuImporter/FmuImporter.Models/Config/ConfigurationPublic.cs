// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Models.Config;

public class ConfigurationPublic
{
  private const int MaxConfigVersion = 2;

  [Required(ErrorMessage = "Version is a required attribute."), Range(1, MaxConfigVersion)]
  public int Version { get; set; } = default!;

  public UInt64? StepSize { get; set; }
  public List<string /* includePath */>? Include { get; set; }
  public List<Parameter>? Parameters { get; set; }
  public List<VariableConfiguration>? VariableMappings { get; set; }
  public bool IgnoreUnmappedVariables { get; set; } = false;

  public bool AlwaysUseStructuredNamingConvention { get; set; } = false;
}
