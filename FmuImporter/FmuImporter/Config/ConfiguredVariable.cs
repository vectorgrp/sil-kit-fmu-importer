// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;

namespace FmuImporter.Config;

public class ConfiguredVariable : ConfiguredVariablePublic
{
  public Variable? FmuVariableDefinition { get; set; }

  // internal data
  public object? SilKitService { get; set; }
}
