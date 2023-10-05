// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class ConfigurationPublic
{
  public int? Version { get; set; }
  public UInt64? StepSize { get; set; }
  public List<string /* includePath */>? Include { get; set; }
  public List<Parameter>? Parameters { get; set; }
  public List<VariableConfiguration>? VariableMappings { get; set; }
  public bool? IgnoreUnmappedVariables { get; set; }
}
