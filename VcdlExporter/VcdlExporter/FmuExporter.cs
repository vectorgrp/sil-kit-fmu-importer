// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using FmuImporter.Fmu;

namespace VcdlExporter;

public class FmuExporter : BaseExporter
{
  public string FmuPath { get; }
  public string VcdlPath { get; }

  public FmuExporter(string fmuPath, string vcdlPath)
  {
    FmuPath = fmuPath;
    VcdlPath = vcdlPath;
  }

  public void Export()
  {
    var commonTextSb = new StringBuilder();
    var interfaceSb = new StringBuilder();
    var objectsSb = new StringBuilder();

    var fmuEntity = new FmuEntity(FmuPath, FmiLogCallback);

    ModelDescription modelDescription;
    switch (fmuEntity.FmiVersion)
    {
      case FmiVersions.Fmi2:
        modelDescription = ((IFmi2Binding)fmuEntity.Binding).ModelDescription;
        AddVcdlHeader(commonTextSb, modelDescription.CoSimulation.ModelIdentifier);
        ParseFmi2(modelDescription, interfaceSb, objectsSb);
        break;
      case FmiVersions.Fmi3:
        modelDescription = ((IFmi3Binding)fmuEntity.Binding).ModelDescription;
        AddVcdlHeader(commonTextSb, modelDescription.CoSimulation.ModelIdentifier);
        ParseFmi3(modelDescription, interfaceSb, objectsSb);
        break;
      case FmiVersions.Invalid:
      default:
        throw new InvalidDataException("The FMU uses an unsupported FMU version.");
    }

    commonTextSb.Append(interfaceSb.ToString());
    commonTextSb.AppendLine(objectsSb.ToString());

    AddVcdlFooter(commonTextSb);

    Console.WriteLine("Writing vCDL to " + VcdlPath);
    File.WriteAllText(VcdlPath, commonTextSb.ToString());
  }

  private void FmiLogCallback(LogSeverity severity, string message)
  {
    Console.WriteLine(message);
  }

  private void ParseFmi2(ModelDescription modelDescription, StringBuilder interfaceSb, StringBuilder objectsSb)
  {
    var vcdlProviderVariables = new HashSet<VcdlVariable>();
    var vcdlConsumerVariables = new HashSet<VcdlVariable>();

    AddEnumerationDefinitions(modelDescription, interfaceSb);

    foreach (var variable in modelDescription.Variables)
    {
      var vValue = variable.Value;
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      string typeString;
      // assume scalar
      if (vValue.Dimensions != null && vValue.Dimensions.Length > 0)
      {
        throw new NotSupportedException(
          $"FMI 2.0.x does not support arrays. Check variable '{vValue.Name}'.");
      }

      if (vValue.VariableType is VariableTypes.EnumFmi2)
      {
        typeString = vValue.TypeDefinition!.Name;
      }
      else
      {
        // assume scalar
        typeString = GetVarTypeString(vValue.VariableType);
      }

      var v = new VcdlVariable()
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
        case Variable.Causalities.Independent:
        case Variable.Causalities.Parameter:
        case Variable.Causalities.StructuralParameter:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Local:
          // ignore
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }
    }

    objectsSb.AppendLine();

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    vcdlConsumerVariables.ExceptWith(vcdlProviderVariables);

    AddInterfaceHeader(modelDescription.CoSimulation.ModelIdentifier, null, interfaceSb);

    AddInterfaceBody(vcdlProviderVariables, VcdlCausality.Provider, interfaceSb);
    if (vcdlProviderVariables.Count > 0 && vcdlConsumerVariables.Count > 0)
    {
      interfaceSb.AppendLine();
    }

    AddInterfaceBody(vcdlConsumerVariables, VcdlCausality.Consumer, interfaceSb);

    AddInterfaceFooter(interfaceSb);

    AddObjectInstance(
      modelDescription.CoSimulation.ModelIdentifier,
      modelDescription.CoSimulation.ModelIdentifier,
      objectsSb);
  }

  private void ParseFmi3(ModelDescription modelDescription, StringBuilder interfaceSb, StringBuilder objectsSb)
  {
    var vcdlProviderVariables = new HashSet<VcdlVariable>();
    var vcdlConsumerVariables = new HashSet<VcdlVariable>();

    AddEnumerationDefinitions(modelDescription, interfaceSb);

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
        if (vValue.VariableType is VariableTypes.EnumFmi3)
        {
          typeString = vValue.TypeDefinition!.Name;
        }
        else
        {
          typeString = GetVarTypeString(vValue.VariableType);
        }
      }
      else
      {
        // assume array of scalar
        typeString = $"list<{GetVarTypeString(vValue.VariableType)}> ";
      }

      var v = new VcdlVariable()
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
        case Variable.Causalities.Independent:
        case Variable.Causalities.Parameter:
        case Variable.Causalities.StructuralParameter:
          vcdlConsumerVariables.Add(v);
          break;
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Local:
          // ignore
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }
    }

    objectsSb.AppendLine();

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    vcdlConsumerVariables.ExceptWith(vcdlProviderVariables);

    AddInterfaceHeader($"{modelDescription.CoSimulation.ModelIdentifier}", null, interfaceSb);

    AddInterfaceBody(vcdlProviderVariables, VcdlCausality.Provider, interfaceSb);
    if (vcdlProviderVariables.Count > 0 && vcdlConsumerVariables.Count > 0)
    {
      interfaceSb.AppendLine();
    }

    AddInterfaceBody(vcdlConsumerVariables, VcdlCausality.Consumer, interfaceSb);

    AddInterfaceFooter(interfaceSb);

    AddObjectInstance(
      $"{modelDescription.CoSimulation.ModelIdentifier}",
      modelDescription.CoSimulation.ModelIdentifier,
      objectsSb);
  }

  private string GetVarTypeString(VariableTypes variableType)
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

  private void AddEnumerationDefinitions(ModelDescription modelDescription, StringBuilder sb)
  {
    foreach (var typedefinition in modelDescription.TypeDefinitions)
    {
      if (typedefinition.Value.EnumerationValues != null)
      {
        sb.AppendLine($"  enum {typedefinition.Key.Replace(' ', '_')} : int64");
        sb.AppendLine("  {");
        foreach (var val in typedefinition.Value.EnumerationValues)
        {
          sb.AppendLine($"    {val.Item1.Replace(' ', '_')} = {val.Item2},");
        }

        sb.AppendLine("  }\n");
      }
    }
  }
}
