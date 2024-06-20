﻿// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Models.Config;

public class TransformationPublic
{
  public double? Offset { get; set; }
  public double? Factor { get; set; }
  public string? TransmissionType { get; set; }
  public bool? ReverseTransform { get; set; }
}
