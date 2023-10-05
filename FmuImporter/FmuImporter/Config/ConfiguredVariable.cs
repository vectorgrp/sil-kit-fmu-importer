// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;

namespace FmuImporter.Config;

public class ConfiguredVariable
{
  public ConfiguredVariable(VariableConfiguration importerVariableConfiguration, Variable fmuVariableDefinition)
  {
    ImporterVariableConfiguration = importerVariableConfiguration;
    FmuVariableDefinition = fmuVariableDefinition;
  }

  public VariableConfiguration ImporterVariableConfiguration { get; set; }
  public Variable FmuVariableDefinition { get; set; }
}
