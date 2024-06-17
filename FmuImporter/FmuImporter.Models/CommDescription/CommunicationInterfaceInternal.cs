// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Linq;
using FmuImporter.Models.Exceptions;

namespace FmuImporter.Models.CommDescription;

public class CommunicationInterfaceInternal : CommunicationInterface
{
  public void CheckAndAssignCustomTypeDependencies()
  {
    var hasStructDefinitions = StructDefinitions != null;
    var hasEnumDefinitions = EnumDefinitions != null;

    var enumDefinitions = hasEnumDefinitions ? EnumDefinitions!.ToDictionary(def => def.Name) : null;
    var structDefinitions = hasStructDefinitions ? StructDefinitions!.ToDictionary(def => def.Name) : null;

    if (hasStructDefinitions)
    {
      // iterate through all structure definitions and resolve custom data types (if applicable)
      foreach (var structDefinition in StructDefinitions!)
      {
        var needsStructDefinitions = false;
        var needsEnumDefinitions = false;
        foreach (var structMember in structDefinition.Members)
        {
          if (structMember.ResolvedType.Type == null &&
              !string.IsNullOrEmpty(structMember.ResolvedType.CustomTypeName))
          {
            // Search for custom type name in definitions - otherwise throw an exception
            // First, search all structure definitions...
            var success = structDefinitions!.TryGetValue(
              structMember.ResolvedType.CustomTypeName,
              out var externalStructDefinition);
            if (success)
            {
              structMember.ResolvedType.CustomType = externalStructDefinition;
              needsStructDefinitions = true;
              continue;
            }
            else
            {
              // ... then, search all enum definitions
              if (hasEnumDefinitions)
              {
                success = enumDefinitions!.TryGetValue(
                  structMember.ResolvedType.CustomTypeName,
                  out var externalEnumDefinition);
                if (success)
                {
                  structMember.ResolvedType.CustomType = externalEnumDefinition;
                  needsEnumDefinitions = true;
                  continue;
                }
              }

              throw new InvalidCommunicationInterfaceException(
                $"Failed to match data type: structure definition='{structDefinition.Name}'; " +
                $"member='{structMember.Type}'; data type='{structMember.Type}'.");
            }
          }
        }

        if (needsStructDefinitions)
        {
          structDefinition.ExternalStructDefinitions = structDefinitions;
        }

        if (needsEnumDefinitions)
        {
          structDefinition.ExternalEnumDefinitions = enumDefinitions;
        }
      }
    }

    if (Publishers != null)
    {
      foreach (var publisher in Publishers)
      {
        if (publisher.ResolvedType.Type == null)
        {
          string customTypeName;

          if (!string.IsNullOrEmpty(publisher.ResolvedType.CustomTypeName))
          {
            customTypeName = publisher.ResolvedType.CustomTypeName;
          }
          // TODO this assumes that there are no nested classes
          // this must be fixed if this ever changes
          else if (publisher.ResolvedType.IsList == true &&
                   publisher.ResolvedType.InnerType!.Type == null &&
                   !string.IsNullOrEmpty(publisher.ResolvedType.InnerType!.CustomTypeName))
          {
            customTypeName = publisher.ResolvedType.InnerType!.CustomTypeName;
          }
          else
          {
            continue;
          }

          // Search for custom type name in definitions - otherwise throw an exception
          // First, search all structure definitions (if applicable)...
          if (hasStructDefinitions)
          {
            var success = structDefinitions!.TryGetValue(
              customTypeName,
              out var externalStructDefinition);
            if (success)
            {
              if (publisher.ResolvedType.IsList == true)
              {
                publisher.ResolvedType.InnerType!.CustomType = externalStructDefinition;
              }
              else
              {
                publisher.ResolvedType.CustomType = externalStructDefinition;
              }

              continue;
            }
          }

          // ... then, search all enum definitions (if applicable)
          if (hasEnumDefinitions)
          {
            var success = enumDefinitions!.TryGetValue(
              customTypeName,
              out var externalEnumDefinition);
            if (success)
            {
              //publisher.ResolvedType.CustomType = externalEnumDefinition;
              if (publisher.ResolvedType.IsList == true)
              {
                publisher.ResolvedType.InnerType!.CustomType = externalEnumDefinition;
              }
              else
              {
                publisher.ResolvedType.CustomType = externalEnumDefinition;
              }

              continue;
            }
          }

          throw new InvalidCommunicationInterfaceException(
            $"Failed to match data type: publisher='{publisher.Name}'; " +
            $"data type='{publisher.Type}'.");
        }
      }
    }

    if (Subscribers != null)
    {
      foreach (var subscriber in Subscribers)
      {
        if (subscriber.ResolvedType.Type == null && !string.IsNullOrEmpty(subscriber.ResolvedType.CustomTypeName))
        {
          // Search for custom type name in definitions - otherwise throw an exception
          // First, search all structure definitions (if applicable)...
          if (hasStructDefinitions)
          {
            var success = structDefinitions!.TryGetValue(
              subscriber.ResolvedType.CustomTypeName,
              out var externalStructDefinition);
            if (success)
            {
              if (subscriber.ResolvedType.IsList == true)
              {
                subscriber.ResolvedType.InnerType!.CustomType = externalStructDefinition;
              }
              else
              {
                subscriber.ResolvedType.CustomType = externalStructDefinition;
              }

              continue;
            }
          }

          // ... then, search all enum definitions (if applicable)
          if (hasEnumDefinitions)
          {
            var success = enumDefinitions!.TryGetValue(
              subscriber.ResolvedType.CustomTypeName,
              out var externalEnumDefinition);
            if (success)
            {
              if (subscriber.ResolvedType.IsList == true)
              {
                subscriber.ResolvedType.InnerType!.CustomType = externalEnumDefinition;
              }
              else
              {
                subscriber.ResolvedType.CustomType = externalEnumDefinition;
              }

              continue;
            }
          }

          throw new InvalidCommunicationInterfaceException(
            $"Failed to match data type: subscriber='{subscriber.Name}'; " +
            $"data type='{subscriber.Type}'.");
        }
      }
    }
  }
}
