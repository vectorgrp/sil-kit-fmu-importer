// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using FmuImporter.Fmu;

namespace VcdlExporter;

internal class Program
{
  private enum Causality
  {
    Provider,
    Consumer
  }

  private class FmiVariable
  {
    public string? Name { get; set; }
    public string? Type { get; set; }
  }

  private static void Main(string[] args)
  {
    var outputFile = args[0];

    var commonTextSb = new StringBuilder();
    var interfaceSb = new StringBuilder();
    var objectsSb = new StringBuilder();

    commonTextSb.AppendLine("version 2.0;\nimport module \"SilKit\";\n");
    commonTextSb.AppendLine($"namespace {typeof(Program).Namespace}\n{{");

    for (var i = 1; i < args.Length; i++)
    {
      var inputFile = args[i];

      var fmuEntity = new FmuEntity(inputFile);

      switch (fmuEntity.FmiVersion)
      {
        case FmiVersions.Fmi2:
          ParseFmi2((IFmi2Binding)fmuEntity.Binding, interfaceSb, objectsSb);
          break;
        case FmiVersions.Fmi3:
          ParseFmi3((IFmi3Binding)fmuEntity.Binding, interfaceSb, objectsSb);
          break;
        case FmiVersions.Invalid:
        default:
          throw new InvalidDataException("The FMU uses an unsupported FMU version.");
      }
    }

    commonTextSb.Append(interfaceSb.ToString());
    commonTextSb.AppendLine(objectsSb.ToString());
    commonTextSb.AppendLine("}");

    Console.WriteLine("Writing vCDL to " + outputFile);
    File.WriteAllText(outputFile, commonTextSb.ToString());
  }

  private static void ParseFmi2(IFmi2Binding binding, StringBuilder interfaceSb, StringBuilder objectsSb)
  {
    var modelDescription = binding.GetModelDescription();

    var vcdlProviderVariables = new HashSet<FmiVariable>();
    var vcdlConsumerVariables = new HashSet<FmiVariable>();

    foreach (var variable in modelDescription.Variables)
    {
      // TODO remove this block once enumeration export is supported
      if (variable.Value.VariableType is VariableTypes.EnumFmi2 or VariableTypes.EnumFmi3)
      {
        continue;
      }

      var vValue = variable.Value;
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      string typeString;
      if (vValue.Dimensions == null || vValue.Dimensions.Length == 0)
      {
        // assume scalar
        typeString = GetVarTypeString(vValue.VariableType) + " ";
      }
      else
      {
        // assume array of scalar
        typeString = $"list<{GetVarTypeString(vValue.VariableType)}> ";
      }

      var v = new FmiVariable()
      {
        Name = vValue.Name,
        Type = typeString
      };

      switch (vValue.Causality)
      {
        case Variable.Causalities.Input:
          vcdlProviderVariables.Add(v);
          break;
        case Variable.Causalities.Output:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.Independent:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.Parameter:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Local:
        case Variable.Causalities.StructuralParameter:
          // ignore
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }
    }

    objectsSb.AppendLine();

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    vcdlConsumerVariables.ExceptWith(vcdlProviderVariables);

    var headerSb = GenerateInterfaceHeader($"I{modelDescription.CoSimulation.ModelIdentifier}");

    var bodySb = new StringBuilder();
    var providerSbRaw = GenerateInterfaceBody(vcdlProviderVariables, Causality.Provider);
    if (providerSbRaw != null)
    {
      bodySb.Append(providerSbRaw.ToString());
    }

    var consumerSbRaw = GenerateInterfaceBody(vcdlConsumerVariables, Causality.Consumer);
    if (consumerSbRaw != null)
    {
      if (providerSbRaw != null)
      {
        bodySb.AppendLine();
      }

      bodySb.Append(consumerSbRaw.ToString());
    }

    var footerSb = GenerateInterfaceFooter();

    interfaceSb.Append(headerSb.ToString());
    interfaceSb.Append(bodySb.ToString());
    interfaceSb.AppendLine(footerSb.ToString());

    objectsSb.AppendLine(
      $"  I{modelDescription.CoSimulation.ModelIdentifier} {modelDescription.CoSimulation.ModelIdentifier};");
  }

  private static void ParseFmi3(IFmi3Binding binding, StringBuilder interfaceSb, StringBuilder objectsSb)
  {
    var modelDescription = binding.GetModelDescription();

    var vcdlProviderVariables = new HashSet<FmiVariable>();
    var vcdlConsumerVariables = new HashSet<FmiVariable>();

    foreach (var variable in modelDescription.Variables)
    {
      var vValue = variable.Value;
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      string typeString;
      if (vValue.Dimensions == null || vValue.Dimensions.Length == 0)
      {
        // assume scalar
        typeString = GetVarTypeString(vValue.VariableType) + " ";
      }
      else
      {
        // assume array of scalar
        typeString = $"list<{GetVarTypeString(vValue.VariableType)}> ";
      }

      var v = new FmiVariable()
      {
        Name = vValue.Name,
        Type = typeString
      };
      switch (vValue.Causality)
      {
        case Variable.Causalities.Input:
          vcdlProviderVariables.Add(v);
          break;
        case Variable.Causalities.Output:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.Independent:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.Parameter:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Local:
        case Variable.Causalities.StructuralParameter:
          // ignore
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }
    }

    objectsSb.AppendLine();

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    vcdlConsumerVariables.ExceptWith(vcdlProviderVariables);

    var headerSb = GenerateInterfaceHeader($"I{modelDescription.CoSimulation.ModelIdentifier}");

    var bodySb = new StringBuilder();
    var providerSbRaw = GenerateInterfaceBody(vcdlProviderVariables, Causality.Provider);
    if (providerSbRaw != null)
    {
      bodySb.Append(providerSbRaw.ToString());
    }

    var consumerSbRaw = GenerateInterfaceBody(vcdlConsumerVariables, Causality.Consumer);
    if (consumerSbRaw != null)
    {
      if (providerSbRaw != null)
      {
        bodySb.AppendLine();
      }

      bodySb.Append(consumerSbRaw.ToString());
    }

    var footerSb = GenerateInterfaceFooter();

    interfaceSb.Append(headerSb.ToString());
    interfaceSb.Append(bodySb.ToString());
    interfaceSb.AppendLine(footerSb.ToString());

    objectsSb.Append(
      $"  I{modelDescription.CoSimulation.ModelIdentifier} {modelDescription.CoSimulation.ModelIdentifier};");
  }

  private static StringBuilder GenerateInterfaceHeader(string interfaceName)
  {
    var interfaceSb = new StringBuilder();

    interfaceSb.AppendLine("  [Binding=\"SilKit\"]");
    interfaceSb.AppendLine($"  interface {interfaceName}\n  {{");
    return interfaceSb;
  }

  private static StringBuilder? GenerateInterfaceBody(HashSet<FmiVariable> variables, Causality causality)
  {
    if (variables.Count == 0)
    {
      return null;
    }

    var interfaceSb = new StringBuilder();

    foreach (var variable in variables)
    {
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      switch (causality)
      {
        case Causality.Provider:
          interfaceSb.Append("    provided data ");
          break;
        case Causality.Consumer:
          interfaceSb.Append("    consumed data ");
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(causality), causality, null);
      }

      interfaceSb.Append(variable.Type);
      interfaceSb.AppendLine($"{variable.Name};");
    }

    return interfaceSb;
  }

  private static StringBuilder GenerateInterfaceFooter()
  {
    var interfaceSb = new StringBuilder();
    interfaceSb.AppendLine("  }");

    return interfaceSb;
  }

  private static string GetVarTypeString(VariableTypes variableType)
  {
    var typeName = variableType.ToString().ToLowerInvariant();
    // replace types that do not match vCDL
    typeName = typeName.Replace("single", "float");
    typeName = typeName.Replace("sbyte", "int8");
    typeName = typeName.Replace("byte", "uint8");
    typeName = typeName.Replace("intptr", "bytes");
    typeName = typeName.Replace("boolean", "bool");
    typeName = typeName.Replace("binary", "bytes");
    // FMI 2.0.x types
    typeName = typeName.Replace("float32", "float");
    typeName = typeName.Replace("float64", "double");
    return typeName;
  }
}
