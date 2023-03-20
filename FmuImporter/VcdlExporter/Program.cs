using System.Text;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace VcdlExporter;

internal class Program
{
  static void Main(string[] args)
  {
    string outputFile = args[0];

    var commonTextSb = new StringBuilder();
    var interfaceSb = new StringBuilder();
    var objectsSb = new StringBuilder();

    commonTextSb.AppendLine("version 2.0;\nimport module \"SilKit\";\n\n");
    commonTextSb.AppendLine($"namespace {typeof(Program).Namespace}\n{{");

    for (int i = 1; i < args.Length; i++)
    {
      string inputFile = args[i];

      interfaceSb.AppendLine("  [Binding=\"SilKit\"]");

      switch (ModelLoader.FindFmiVersion(inputFile))
      {
        case ModelLoader.FmiVersions.Fmi2:
          ParseFmi2(Fmi2BindingFactory.CreateFmi2Binding(inputFile), interfaceSb, objectsSb);
          break;
        case ModelLoader.FmiVersions.Fmi3:
          ParseFmi3(Fmi3BindingFactory.CreateFmi3Binding(inputFile), interfaceSb, objectsSb);
          break;
        case ModelLoader.FmiVersions.Invalid:
        default:
          throw new InvalidDataException($"The FMU uses an unsupported FMU version.");
      }
    }

    commonTextSb.Append(interfaceSb.ToString());
    commonTextSb.Append(objectsSb.ToString());
    commonTextSb.AppendLine("}");

    Console.WriteLine("Writing vCDL to " + outputFile);
    File.WriteAllText(outputFile, commonTextSb.ToString());
  }

  private static void ParseFmi2(IFmi2Binding binding, StringBuilder interfaceSb, StringBuilder objectsSb)
  {
    var modelDescription = binding.GetModelDescription();

    interfaceSb.AppendLine($"  interface I{modelDescription.CoSimulation.ModelIdentifier}\n  {{");

    foreach (var variable in modelDescription.Variables)
    {
      var vValue = variable.Value;
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      switch (vValue.Causality)
      {
        case Variable.Causalities.Input:
          interfaceSb.Append("    provided data ");
          break;
        case Variable.Causalities.Output:
          interfaceSb.Append("    consumed data ");
          break;
        case Variable.Causalities.Independent:
          break;
        case Variable.Causalities.Parameter:
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Local:
        case Variable.Causalities.StructuralParameter:
          // ignore
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }

      if (vValue.Dimensions?.Length == 0)
      {
        // assume scalar
        interfaceSb.Append(GetVarTypeString(vValue.VariableType) + " ");
      }
      else
      {
        // assume array of scalar
        interfaceSb.Append($"list<{GetVarTypeString(vValue.VariableType)}> ");
      }

      // special handling for keywords
      if (vValue.Name.ToLowerInvariant().Trim() == "time")
      {
        interfaceSb.AppendLine($"t;");
        Console.WriteLine("Replaced variable name 'time' with 't'.");
      }
      else
      {
        interfaceSb.AppendLine($"{vValue.Name};");
      }
    }

    interfaceSb.AppendLine($"  }}\n");
    objectsSb.AppendLine(
      $"  object {modelDescription.CoSimulation.ModelIdentifier}: I{modelDescription.CoSimulation.ModelIdentifier}");
  }

  private static void ParseFmi3(IFmi3Binding binding, StringBuilder interfaceSb, StringBuilder objectsSb)
  {
    var modelDescription = binding.GetModelDescription();

    interfaceSb.AppendLine($"  interface I{modelDescription.CoSimulation.ModelIdentifier}\n  {{");

    foreach (var variable in modelDescription.Variables)
    {
      var vValue = variable.Value;
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      switch (vValue.Causality)
      {
        case Variable.Causalities.Input:
          interfaceSb.Append("    provided data ");
          break;
        case Variable.Causalities.Output:
          interfaceSb.Append("    consumed data ");
          break;
        case Variable.Causalities.Independent:
          interfaceSb.Append("    consumed data ");
          break;
        case Variable.Causalities.Parameter:
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Local:
        case Variable.Causalities.StructuralParameter:
          // ignore
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }

      if (vValue.Dimensions?.Length > 0)
      {
        // assume array of scalar
        interfaceSb.Append($"list<{GetVarTypeString(vValue.VariableType)}> ");
      }
      else
      {
        // assume scalar
        interfaceSb.Append(GetVarTypeString(vValue.VariableType) + " ");
      }

      interfaceSb.AppendLine($"{vValue.Name};");
    }

    interfaceSb.AppendLine($"  }}\n");
    objectsSb.AppendLine(
      $"  I{modelDescription.CoSimulation.ModelIdentifier} {modelDescription.CoSimulation.ModelIdentifier};");
  }

  private static string GetVarTypeString(Type variableType)
  {
    var typeName = variableType.Name.ToLowerInvariant();
    // replace types that do not match vCDL
    typeName = typeName.Replace("single", "float");
    typeName = typeName.Replace("sbyte", "int8");
    typeName = typeName.Replace("byte", "uint8");
    typeName = typeName.Replace("intptr", "bytes");
    typeName = typeName.Replace("boolean", "bool");
    return typeName;
  }
}
