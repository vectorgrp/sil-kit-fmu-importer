// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.CommDescription;

public class StructMember : Dictionary<string, string>
{
  public string Name
  {
    get
    {
      return this.First().Key;
    }
  }

  public string Type
  {
    get
    {
      return this.First().Value;
    }
  }
}
