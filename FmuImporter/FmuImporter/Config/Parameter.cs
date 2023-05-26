// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class Parameter
{
  public string VariableName { get; set; }
  public object? Value { get; set; }

  public Parameter()
  {
    VariableName = string.Empty;
  }
}
