// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Supplements;

public class StructuredNameContainer
{
  public StructuredNameContainer(List<string> structuredNameList)
  {
    if (structuredNameList == null || structuredNameList.Count == 0)
    {
      throw new ArgumentException(
        "A structured name container must be initialized with a list that contains at least one element.");
    }

    Path = structuredNameList;
    InstanceName = structuredNameList.First();
    PathWithInstanceName = string.Join(".", structuredNameList);
  }

  public string InstanceName { get; }

  public string PathWithInstanceName { get; }

  public List<string> Path { get; }
}
