// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.CommDescription;

public class CommunicationInterfaceInternal : CommunicationInterface
{
  public void ResolveStructDefinitionDependencies()
  {
    if (StructDefinitions == null)
    {
      return;
    }

    var dict = StructDefinitions.ToDictionary(sd => sd.Name);
    foreach (var commInterfaceStructDefinition in StructDefinitions)
    {
      commInterfaceStructDefinition.ExternalStructDefinitions = dict;
    }
  }
}
