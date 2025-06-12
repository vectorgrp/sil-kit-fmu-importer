// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using FmuImporter.Models.CommDescription;
using FmuImporter.Models.Helpers;

namespace VcdlExporter;

public class CommInterfaceExporter : BaseExporter
{
  public string CommunicationInterfaceDescriptionPath { get; }
  public string VcdlPath { get; }

  private readonly string _interfaceName;
  private readonly string _defaultInstanceName = "FmuInstance";
  private readonly string _defaultNamespace = "FMU";

  public CommInterfaceExporter(string communicationInterfaceDescriptionPath, string vcdlPath, string interfaceName)
  {
    CommunicationInterfaceDescriptionPath = communicationInterfaceDescriptionPath;
    VcdlPath = vcdlPath;
    _interfaceName = interfaceName;
  }

  public void Export()
  {
    var commInterface =
      CommunicationInterfaceDescriptionParser.Load(CommunicationInterfaceDescriptionPath);

    var sb = new StringBuilder();

    // Add vCDL header
    AddVcdlHeader(sb, commInterface.Namespace ?? _defaultNamespace);

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
    vcdlConsumerVariables.ExceptWith(vcdlProviderVariables);

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
    AddObjectInstance(_interfaceName, commInterface.Instance ?? _defaultInstanceName, sb);

    // Add vCDL footer
    AddVcdlFooter(sb);

    Console.WriteLine("Writing vCDL to " + VcdlPath);
    File.WriteAllText(VcdlPath, sb.ToString());
  }


  private void AddEnumerationDefinitions(CommunicationInterface communicationInterface, StringBuilder sb)
  {
    if (communicationInterface.EnumDefinitions == null)
    {
      return;
    }

    foreach (var enumDefinition in communicationInterface.EnumDefinitions)
    {
      sb.AppendLine($"  enum {enumDefinition.Name} : int64");
      sb.AppendLine("  {");
      foreach (var enumDefinitionItem in enumDefinition.Items)
      {
        sb.AppendLine($"    {enumDefinitionItem.Name.Replace(' ', '_')} = {enumDefinitionItem.Value},");
      }

      sb.AppendLine("  }\n");
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
      sb.AppendLine($"  struct {structDefinition.Name}");
      sb.AppendLine("  {");
      foreach (var member in structDefinition.Members)
      {
        if (member.ResolvedType.IsOptional)
        {
          sb.AppendLine($"    [Optional]");
        }

        sb.AppendLine($"    {CreateVariableType(member.ResolvedType)} {member.Name};");
      }

      sb.AppendLine("  }\n");
    }
  }

  internal string CreateVariableType(OptionalType optionalType)
  {
    var sb = new StringBuilder();

    if (optionalType.IsList == true)
    {
      sb.Append("list<");
      sb.Append(
        optionalType.InnerType!.CustomTypeName ??
        CanonizeTokenTypeName(optionalType.InnerType.Type!.Name));
      sb.Append(">");
      return sb.ToString();
    }
    else
    {
      return optionalType.CustomTypeName ??
             CanonizeTokenTypeName(optionalType.Type!.Name); //TODO CHECK regular type name export
    }
  }

  public static string CanonizeTokenTypeName(string input)
  {
    switch (input.ToLowerInvariant())
    {
      case "bool" or "boolean":
        return "bool";
      case "sbyte" or "int8":
        return "int8";
      case "short" or "int16":
        return "int16";
      case "int" or "integer" or "int32":
        return "int32";
      case "long" or "int64":
        return "int64";
      case "byte" or "uint8":
        return "uint8";
      case "ushort" or "uint16":
        return "uint16";
      case "uint" or "uint32":
        return "uint32";
      case "ulong" or "uint64":
        return "uint64";
      case "float" or "float32" or "single":
        return "float";
      case "float64" or "real" or "double":
        return "double";
      case "string":
        return "string";
      case "binary" or "byte[]":
        return "bytes";
    }

    return input;
  }
}
