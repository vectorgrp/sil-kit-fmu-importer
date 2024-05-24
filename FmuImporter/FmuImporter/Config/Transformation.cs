// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Helpers;

namespace FmuImporter.Config;

public class Transformation : TransformationPublic
{
  private double? _computedFactor;

  public double ComputedFactor
  {
    get
    {
      if (_computedFactor == null)
      {
        if (Factor == null)
        {
          _computedFactor = 1.0;
        }
        else if (ReverseTransform ?? false)
        {
          _computedFactor = 1.0 / Factor;
        }
        else
        {
          _computedFactor = Factor;
        }
      }

      return (double)_computedFactor;
    }
  }

  private double? _computedOffset;

  public double ComputedOffset
  {
    get
    {
      if (_computedOffset == null)
      {
        if (Offset == null)
        {
          _computedOffset = 0.0;
        }
        else if (ReverseTransform ?? false)
        {
          _computedOffset = -Offset / Factor ?? 1.0;
        }
        else
        {
          _computedOffset = Offset;
        }
      }

      return (double)_computedOffset;
    }
  }

  public new string? TransmissionType
  {
    get
    {
      return base.TransmissionType;
    }
    set
    {
      base.TransmissionType = value;
      if (value != null)
      {
        ResolvedTransmissionType = Helpers.Helpers.StringToType(value);
      }
    }
  }

  public OptionalType? ResolvedTransmissionType { get; private set; }
}
