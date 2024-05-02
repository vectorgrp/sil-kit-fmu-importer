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
          throw new InvalidCommunicationInterfaceException("Recursive call detected. This is not allowed.");
        }

        _flattenedMembers = new List<StructMemberInternal>();

        foreach (var structMember in Members)
        {
          if (ExternalStructDefinitions != null)
          {
            var success = ExternalStructDefinitions.TryGetValue(
              structMember.Type,
              out var externalStructDefinition);
            if (success)
            {
              var substructMembers = externalStructDefinition!.FlattenedMembers;
              foreach (var structMemberInternal in substructMembers)
              {
                structMemberInternal.QualifiedName = $"{structMember.Name}.{structMemberInternal.QualifiedName}";
              }

              _flattenedMembers.AddRange(substructMembers);
            }
            else
            {
              // TODO / FIXME this assumes that the unknown type is a scalar - this may not always be the case
              var memInternal = new StructMemberInternal(structMember)
              {
                QualifiedName = $"{structMember.Name}"
              };
              _flattenedMembers.Add(memInternal);
            }
          }
        }

        _membersFlattened = true;
      }

      return _flattenedMembers!;
    }
  }

  public Dictionary<string, StructDefinitionInternal>? ExternalStructDefinitions { private get; set; }
}
