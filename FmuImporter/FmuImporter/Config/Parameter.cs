// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class Parameter
{
  public Parameter()
  {
    VariableName = string.Empty;
    Value = new object();
  }

  public string VariableName { get; set; }
  public object Value { get; set; }
}
