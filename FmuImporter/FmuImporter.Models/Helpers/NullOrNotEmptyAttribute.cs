// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace FmuImporter.Models.Helpers;

[AttributeUsage(
  AttributeTargets.Property |
  AttributeTargets.Field,
  AllowMultiple = false)]
public sealed class NullOrNotEmptyAttribute : ValidationAttribute
{
  public override bool IsValid(object? value)
  {
    if (value == null)
    {
      return true;
    }

    var l = value as IList;
    return (l is not null) && l.Count > 0;
  }
}
