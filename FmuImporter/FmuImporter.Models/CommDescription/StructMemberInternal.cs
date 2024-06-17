// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Models.Helpers;

namespace FmuImporter.Models.CommDescription;

public class StructMemberInternal : StructMember
{
  public StructMemberInternal()
  {
  }

  public StructMemberInternal(StructMemberInternal member)
  {
    Name = member.Name;
    Type = member.Type;
    ResolvedType = member.ResolvedType;
    QualifiedName = member.QualifiedName;
  }

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

  public string QualifiedName { get; set; } = default!;

  public OptionalType ResolvedType { get; set; } = default!;
}
