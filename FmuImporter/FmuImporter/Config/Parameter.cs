// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class Parameter
{
  public string VarName { get; set; }
  public string? Description { get; set; }
  public string? Type { get; set; }
  public object? Value { get; set; }

  public Parameter()
  {
    VarName = string.Empty;
  }
}
