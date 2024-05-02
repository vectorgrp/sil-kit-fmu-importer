// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class ConfiguredStructure
{
  public string Name { get; }

  private readonly SortedDictionary<string, ConfiguredVariable?> _sortedStructureMembers;

  public ConfiguredStructure(string structureName, IEnumerable<string> expectedMemberNames)
  {
    Name = structureName;
    _sortedStructureMembers = new SortedDictionary<string, ConfiguredVariable?>();
    foreach (var expectedMemberName in expectedMemberNames)
    {
      _sortedStructureMembers.Add(expectedMemberName, null);
    }
  }

  public void AddMember(ConfiguredVariable configuredVariable)
  {
    var exists = _sortedStructureMembers.ContainsKey(configuredVariable.StructuredPath!.PathWithoutInstanceName);
    if (!exists)
    {
      throw new ArgumentException(
        $"Failed to map '{configuredVariable.StructuredPath.PathWithoutInstanceName}' as a structure member.");
    }

    _sortedStructureMembers[configuredVariable.StructuredPath.PathWithoutInstanceName] = configuredVariable;
  }

  public IEnumerable<ConfiguredVariable?> SortedStructureMembers
  {
    get
    {
      // TODO check if this is properly sorted
      return _sortedStructureMembers.Values.ToList();
    }
  }
}
