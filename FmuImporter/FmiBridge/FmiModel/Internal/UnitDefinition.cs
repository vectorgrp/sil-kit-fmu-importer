// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.FmiModel.Internal;

public class UnitDefinition
{
  public string Name { get; set; } = null!;
  public double? Offset { get; set; }
  public double? Factor { get; set; }
}
