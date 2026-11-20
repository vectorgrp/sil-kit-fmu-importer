// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using FmuImporter.Models.CommDescription;
using FmuImporter.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace VcdlExporter;

public class CommInterfaceExporter : BaseExporter
{
  public string CommunicationInterfaceDescriptionPath { get; }
  public string VcdlPath { get; }

  private readonly string _interfaceName;
  private readonly string _defaultInstanceName = "FmuInstance";
  private readonly string _defaultNamespace = "FMU";
  private readonly ILogger _logger;
  private readonly IdentifierValidator _identifierValidator = new("communication interface description");
  // Original enumeration type name -> name written to the vCDL (see AddEnumerationDefinitions)
  private readonly Dictionary<string, string> _enumTypeNames = new();

  public CommInterfaceExporter(
    string communicationInterfaceDescriptionPath,
    string vcdlPath,
    string interfaceName,
    ILogger logger)
  {
    CommunicationInterfaceDescriptionPath = communicationInterfaceDescriptionPath;
    VcdlPath = vcdlPath;
    _interfaceName = interfaceName;
    _logger = logger;
  }

  public void Export()
  {
    var commInterface =
      CommunicationInterfaceDescriptionParser.Load(CommunicationInterfaceDescriptionPath);

    var sb = new StringBuilder();
    var namespaceName = commInterface.Namespace ?? _defaultNamespace;
    var instanceName = commInterface.Instance ?? _defaultInstanceName;

    _identifierValidator.Check(namespaceName, "Namespace");
    // The interface is written with an 'I' prefix (see AddInterfaceHeader / AddObjectInstance)
    _identifierValidator.Check($"I{_interfaceName}", "Interface name");
    _identifierValidator.Check(instanceName, "Instance name");

    // Add vCDL header
    AddVcdlHeader(sb, namespaceName);

    // Add enum definitions
    AddEnumerationDefinitions(commInterface, sb);

    // Add struct definitions
    AddStructDefinitions(commInterface, sb);

    var vcdlProviderVariables = new HashSet<VcdlVariable>();
    var vcdlConsumerVariables = new HashSet<VcdlVariable>();

    if (commInterface.Publishers != null)
    {
      foreach (var publisherInternal in commInterface.Publishers)
      {
        _identifierValidator.Check(publisherInternal.Name, "Publisher");
        var v = new VcdlVariable()
        {
          Name = publisherInternal.Name,
          Type = CreateVariableType(publisherInternal.ResolvedType)
        };
        // NB: VCDL communication direction is inverted compared to communication interface
        vcdlConsumerVariables.Add(v);
      }
    }

    if (commInterface.Subscribers != null)
    {
      foreach (var subscriberInternal in commInterface.Subscribers)
      {
        _identifierValidator.Check(subscriberInternal.Name, "Subscriber");
        var v = new VcdlVariable()
        {
          Name = subscriberInternal.Name,
          Type = CreateVariableType(subscriberInternal.ResolvedType)
        };
        // NB: VCDL communication direction is inverted compared to communication interface
        vcdlProviderVariables.Add(v);
      }
    }

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    RemoveProvidedVariables(vcdlConsumerVariables, vcdlProviderVariables);

    // Add interface for providers
    AddInterfaceHeader(_interfaceName, null, sb);

    AddInterfaceBody(vcdlProviderVariables, VcdlCausality.Provider, sb);

    if (vcdlProviderVariables.Count > 0 && vcdlConsumerVariables.Count > 0)
    {
      sb.AppendLine();
    }

    AddInterfaceBody(vcdlConsumerVariables, VcdlCausality.Consumer, sb);

    AddInterfaceFooter(sb);

    // Add object instances
    AddObjectInstance(_interfaceName, instanceName, sb);

    // Add vCDL footer
    AddVcdlFooter(sb);

    // Fail before writing anything, so that no invalid vCDL file is left behind
    _identifierValidator.ThrowIfInvalid(_logger);

    _logger.LogInformation("Writing vCDL to '{VcdlPath}'.", VcdlPath);
    File.WriteAllText(VcdlPath, sb.ToString());
  }


  private void AddEnumerationDefinitions(CommunicationInterface communicationInterface, StringBuilder sb)
  {
    if (communicationInterface.EnumDefinitions == null)
    {
      return;
    }

    // Only the enumeration types used by publishers, subscribers or struct members are needed in the vCDL,
    // so unused types are skipped (and not validated), like in the FMU export
    var usedTypeNames = new HashSet<string>();
    foreach (var publisher in communicationInterface.Publishers ?? [])
    {
      AddUsedTypeNames(publisher.ResolvedType, usedTypeNames);
    }

    foreach (var subscriber in communicationInterface.Subscribers ?? [])
    {
      AddUsedTypeNames(subscriber.ResolvedType, usedTypeNames);
    }

    foreach (var structDefinition in communicationInterface.StructDefinitions ?? [])
    {
      foreach (var member in structDefinition.Members)
      {
        AddUsedTypeNames(member.ResolvedType, usedTypeNames);
      }
    }

    // Spaces are replaced by '_', so distinct types (e.g. 'A B' and 'A_B') can end up with the same name
    // Such duplicates are reported as invalid identifiers, so the export fails
    var originalTypeNames = new Dictionary<string, string>();
    foreach (var enumDefinition in communicationInterface.EnumDefinitions.Where(e => usedTypeNames.Contains(e.Name)))
    {
      // Publishers, subscribers and struct members reference the type by its original name,
      // which GetCustomTypeName maps to the name written here
      var typeName = enumDefinition.Name.Replace(' ', '_');
      _enumTypeNames[enumDefinition.Name] = typeName;
      if (typeName != enumDefinition.Name)
      {
        _logger.LogInformation(
          "Enumeration type '{OriginalName}' has been renamed to '{TypeName}'.",
          enumDefinition.Name,
          typeName);
      }

      _identifierValidator.Check(typeName, "Enumeration type");
      if (!originalTypeNames.TryAdd(typeName, enumDefinition.Name))
      {
        _identifierValidator.ReportDuplicate(
          typeName, originalTypeNames[typeName], enumDefinition.Name, "Enumeration type");
      }

      sb.AppendLine($"  enum {typeName} : int64");
      sb.AppendLine("  {");
      // Spaces are replaced by '_', so distinct items (e.g. 'A B' and 'A_B') can end up with the same name.
      // Such duplicates are reported as invalid identifiers, so the export fails.
      var originalItemNames = new Dictionary<string, string>();
      foreach (var enumDefinitionItem in enumDefinition.Items)
      {
        var itemName = enumDefinitionItem.Name.Replace(' ', '_');
        if (itemName != enumDefinitionItem.Name)
        {
          _logger.LogInformation(
            "Item '{OriginalName}' of enumeration type '{TypeName}' has been renamed to '{ItemName}'.",
            enumDefinitionItem.Name,
            enumDefinition.Name,
            itemName);
        }

        var itemKind = $"Item of enumeration type '{enumDefinition.Name}'";
        _identifierValidator.Check(itemName, itemKind);
        if (!originalItemNames.TryAdd(itemName, enumDefinitionItem.Name))
        {
          _identifierValidator.ReportDuplicate(itemName, originalItemNames[itemName], enumDefinitionItem.Name, itemKind);
        }

        sb.AppendLine($"    {itemName} = {enumDefinitionItem.Value},");
      }

      sb.AppendLine("  }\n");
    }
  }

  internal override string GetCustomTypeName(string customTypeName)
  {
    return _enumTypeNames.GetValueOrDefault(customTypeName, customTypeName);
  }

  // Adds the custom type name of a type, or of its element type for (nested) lists
  private static void AddUsedTypeNames(OptionalType type, HashSet<string> usedTypeNames)
  {
    if (type.IsList == true)
    {
      AddUsedTypeNames(type.InnerType!, usedTypeNames);
    }
    else if (type.CustomTypeName != null)
    {
      usedTypeNames.Add(type.CustomTypeName);
    }
  }

  private void AddStructDefinitions(CommunicationInterface communicationInterface, StringBuilder sb)
  {
    if (communicationInterface.StructDefinitions == null)
    {
      return;
    }

    foreach (var structDefinition in communicationInterface.StructDefinitions)
    {
      _identifierValidator.Check(structDefinition.Name, "Structure type");
      sb.AppendLine($"  struct {structDefinition.Name}");
      sb.AppendLine("  {");
      foreach (var member in structDefinition.Members)
      {
        if (member.ResolvedType.IsOptional)
        {
          sb.AppendLine($"    [Optional]");
        }

        _identifierValidator.Check(member.Name, $"Member of structure type '{structDefinition.Name}'");
        sb.AppendLine($"    {CreateVariableType(member.ResolvedType)} {member.Name};");
      }

      sb.AppendLine("  }\n");
    }
  }

}
