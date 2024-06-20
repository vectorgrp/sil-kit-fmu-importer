// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;

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
}
