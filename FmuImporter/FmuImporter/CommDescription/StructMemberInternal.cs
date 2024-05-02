// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.CommDescription;

public class StructMemberInternal
{
  public Dictionary<string, string> Member { get; }

  public StructMemberInternal(Dictionary<string, string> member)
  {
    Member = member;
    // TODO fix this!
  }

  public string QualifiedName { get; set; } = default!;

  public string Name
  {
    get
    {
      return Member.First().Key;
    }
  }

  public string Type
  {
    get
    {
      return Member.First().Value;
    }
  }
}
