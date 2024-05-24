// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.CommDescription;

namespace FmuImporter.Helpers;

public class OptionalType
{
  public OptionalType? InnerType { get; }
  public Type? Type { get; }
  public object? CustomType { get; set; }
  public string? CustomTypeName { get; }
  public bool IsOptional { get; }
  public bool IsList { get; }

  private OptionalType(bool isOptional, bool isList)
  {
    IsOptional = isOptional;
    IsList = isList;

    CustomType = null;
    CustomTypeName = null;
    InnerType = null;
    Type = null;
  }

  public OptionalType(bool isOptional, bool isList, OptionalType optionalType)
    : this(isOptional, isList)
  {
    InnerType = optionalType;
  }

  public OptionalType(bool isOptional, bool isList, string customTypeName)
    : this(isOptional, isList)
  {
    CustomTypeName = customTypeName;
  }

  public OptionalType(bool isOptional, bool isList, Type type)
    : this(isOptional, isList)
  {
    Type = type;
  }

  public OptionalType(bool isOptional, bool isList, object customType)
    : this(isOptional, isList)
  {
    if (customType is EnumDefinition enumDefinition)
    {
      CustomTypeName = enumDefinition.Name;
      CustomType = enumDefinition;
    }
    else if (customType is StructDefinitionInternal structDefinition)
    {
      CustomTypeName = structDefinition.Name;
      CustomType = structDefinition;
    }
  }
}
