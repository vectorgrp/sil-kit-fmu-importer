// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Models.Helpers;

namespace FmuImporter.Models.CommDescription;

public class SubscriberInternal : Subscriber
{
  public new string Type
  {
    get
    {
      return base.Type;
    }
    set
    {
      base.Type = value;
      ResolvedType = Helpers.Helpers.StringToType(Type);
    }
  }

  public OptionalType ResolvedType { get; private set; } = default!;
}
