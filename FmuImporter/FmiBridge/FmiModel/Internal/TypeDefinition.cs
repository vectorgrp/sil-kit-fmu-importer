// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.FmiModel.Internal;

public class TypeDefinition
{
  public string Name { get; set; } = null!;
  public UnitDefinition? Unit { get; set; }
}
