// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.CommDescription;

public class StructMemberInternal : StructMember
{
  public StructMemberInternal(StructMember member)
  {
    Name = member.Name;
    Type = member.Type;
  }

  public string QualifiedName { get; set; } = default!;
}
