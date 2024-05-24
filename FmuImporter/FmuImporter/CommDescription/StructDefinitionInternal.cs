// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using FmuImporter.Exceptions;

namespace FmuImporter.CommDescription;

public class StructDefinitionInternal : StructDefinition
{
  private List<StructMemberInternal>? _flattenedMembers;
  private bool _membersFlattened = false;

  // Depth first
  public List<StructMemberInternal> FlattenedMembers
  {
    get
    {
      if (!_membersFlattened)
      {
        if (_flattenedMembers != null)
        {
          throw new InvalidCommunicationInterfaceException(
            "Infinite recursion in structure definition detected. This is not allowed.");
        }

        _flattenedMembers = new List<StructMemberInternal>();

        foreach (var structMember in Members)
        {
          // Lists cannot contain custom types -> add them assuming a built-in or enum type
          if (structMember.ResolvedType.Type != null || structMember.ResolvedType.IsList)
          {
            var flatMember = new StructMemberInternal(structMember)
            {
              QualifiedName = $"{structMember.Name}"
            };
            _flattenedMembers.Add(flatMember);
            continue;
          }

          if (ExternalStructDefinitions != null)
          {
            var success = ExternalStructDefinitions.TryGetValue(
              structMember.Type,
              out var externalStructDefinition);
            if (success)
            {
              var substructMembers = externalStructDefinition!.FlattenedMembers;
              foreach (var substructMember in substructMembers)
              {
                var flatMember = new StructMemberInternal(substructMember)
                {
                  QualifiedName = $"{structMember.Name}.{substructMember.QualifiedName}"
                };

                _flattenedMembers.Add(flatMember);
              }

              continue;
            }
          }

          if (ExternalEnumDefinitions != null)
          {
            var success = ExternalEnumDefinitions.TryGetValue(
              structMember.Type,
              out var externalEnumDefinition);
            if (success)
            {
              var flatMember = new StructMemberInternal(structMember)
              {
                QualifiedName = $"{structMember.Name}"
              };
              _flattenedMembers.Add(flatMember);
              continue;
            }
          }

          throw new InvalidCommunicationInterfaceException(
            $"Failed to process the member {structMember.Name} of structure definition '{Name}'.");
        }

        _membersFlattened = true;
      }

      return _flattenedMembers!;
    }
  }

  public Dictionary<string, StructDefinitionInternal>? ExternalStructDefinitions { private get; set; }
  public Dictionary<string, EnumDefinition>? ExternalEnumDefinitions { private get; set; }
}
