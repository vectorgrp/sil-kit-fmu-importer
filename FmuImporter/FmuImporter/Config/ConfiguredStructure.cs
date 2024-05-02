// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class ConfiguredStructure
{
  public string Name { get; }

  // NB: this currently relies on the .net implementation of Dictionary, which uses lists of key/value pairs
  // (and therefore .Values returns the order in which the members were added)
  private readonly IDictionary<string, ConfiguredVariable?> _structureMembers;
  private static long sStructureId;

  public ConfiguredStructure(string structureName, IEnumerable<string> expectedMemberNames)
  {
    Name = structureName;
    StructureId = long.MaxValue - sStructureId++;
    _structureMembers = new Dictionary<string, ConfiguredVariable?>();
    foreach (var expectedMemberName in expectedMemberNames)
    {
      _structureMembers.Add(expectedMemberName, null);
    }
  }

  public void AddMember(ConfiguredVariable configuredVariable)
  {
    var exists = _structureMembers.ContainsKey(configuredVariable.StructuredPath!.PathWithoutInstanceName);
    if (!exists)
    {
      throw new ArgumentException(
        $"Failed to map '{configuredVariable.StructuredPath.PathWithoutInstanceName}' as a structure member.");
    }

    _structureMembers[configuredVariable.StructuredPath.PathWithoutInstanceName] = configuredVariable;
  }

  public IEnumerable<ConfiguredVariable?> StructureMembers
  {
    get
    {
      return _structureMembers.Values.ToList();
    }
  }

  public long StructureId { get; }
}
