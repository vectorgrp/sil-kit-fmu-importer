// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Config;

public class ConfigurationPublic
{
  private const int MaxConfigVersion = 1;

  [Required, Range(1, MaxConfigVersion)]
  public int Version { get; set; } = default!;

  public UInt64? StepSize { get; set; }
  public List<string /* includePath */>? Include { get; set; }
  public List<Parameter>? Parameters { get; set; }
  public List<VariableConfiguration>? VariableMappings { get; set; }
  public bool IgnoreUnmappedVariables { get; set; } = false;
}
