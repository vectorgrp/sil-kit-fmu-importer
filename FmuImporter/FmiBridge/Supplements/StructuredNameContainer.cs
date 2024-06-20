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
    RootName = structuredNameList.First();
    PathWithRootName = string.Join(".", structuredNameList);
  }

  public string RootName { get; }

  public string PathWithRootName { get; }

  public List<string> Path { get; }
}
