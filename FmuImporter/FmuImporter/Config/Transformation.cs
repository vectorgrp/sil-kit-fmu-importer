// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class Transformation
{
  public double? Offset { get; set; }
  public double? Factor { get; set; }
  public string? TransmissionType { get; set; }
}
