// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using FmuImporter.Models.Helpers;

namespace VcdlExporter;

public abstract class BaseExporter
{
  internal class VcdlVariable
  {
    public string? Name { get; set; }
    public string? Type { get; set; }
  }

  internal enum VcdlCausality
  {
    Provider,
    Consumer
  }

  internal void AddVcdlHeader(StringBuilder sb)
  {
    AddVcdlHeader(sb, typeof(Program).Namespace!);
  }

  internal void AddVcdlHeader(StringBuilder sb, string ns)
  {
    sb.AppendLine("version 2.0;\nimport module \"SilKit\";\n");
    sb.AppendLine($"namespace {ns}\n{{");
  }

  internal void AddVcdlFooter(StringBuilder sb)
  {
    sb.AppendLine("}");
  }

  internal void AddInterfaceHeader(string interfaceName, VcdlCausality? causality, StringBuilder sb)
  {
    var ifName = (causality == null)
                   ? $"I{interfaceName}"
                   : $"I{interfaceName}_{causality.ToString()}";
    sb.AppendLine("  [Binding=\"SilKit\"]");
    sb.AppendLine($"  interface {ifName}\n  {{");
  }

  internal void AddInterfaceBody(HashSet<VcdlVariable> variables, VcdlCausality vcdlCausality, StringBuilder sb)
  {
    if (variables.Count == 0)
    {
      return;
    }

    foreach (var variable in variables)
    {
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      switch (vcdlCausality)
      {
        case VcdlCausality.Provider:
          sb.Append("    provided data ");
          break;
        case VcdlCausality.Consumer:
          sb.Append("    consumed data ");
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(vcdlCausality), vcdlCausality, null);
      }

      sb.AppendLine($"{variable.Type} {variable.Name};");
    }
  }

  internal void AddInterfaceFooter(StringBuilder sb)
  {
    sb.AppendLine("  }");
  }

  internal void AddObjectInstance(string interfaceName, string instanceName, StringBuilder sb)
  {
    sb.AppendLine(
      $"  I{interfaceName} {instanceName};");
  }

  // Single vCDL type renderer, shared by every exporter so that a type is turned into a vCDL type string
  // in exactly one place. Nested lists (i.e. multi-dimensional arrays) render as list<list<...>>.
  internal string CreateVariableType(OptionalType optionalType)
  {
    if (optionalType.IsList == true)
    {
      // Recurse into the inner type so that nested lists (i.e. multi-dimensional arrays)
      // render correctly, e.g. list<list<double>>.
      return $"list<{CreateVariableType(optionalType.InnerType!)}>";
    }

    return optionalType.CustomTypeName ??
           CanonizeTokenTypeName(optionalType.Type!.Name); //TODO CHECK regular type name export
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
