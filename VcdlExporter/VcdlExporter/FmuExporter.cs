// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
using System.Text;
using Fmi;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using FmuImporter.Models.Helpers;

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


  // Returns the first .vcdl file located in the FMU's "resources" folder (or any of its subfolders), if any
  private static ZipArchiveEntry? FindVcdlEntry(ZipArchive fmuArchive)
  {
    return fmuArchive.Entries.FirstOrDefault(
      entry => entry.FullName.Replace('\\', '/').StartsWith("resources/", StringComparison.Ordinal) &&
               entry.FullName.EndsWith(".vcdl", StringComparison.OrdinalIgnoreCase));
  }

  public void Export(bool useClockPubSubElements)
  {
    var commonTextSb = new StringBuilder();
    var interfaceSb = new StringBuilder();
    var objectsSb = new StringBuilder();

    var fmiVersion = ModelLoader.FindFmiVersion(FmuPath);
    if (fmiVersion is not (FmiVersions.Fmi2 or FmiVersions.Fmi3))
    {
      throw new InvalidDataException("The FMU uses an unsupported FMU version.");
    }

    // Only the model description is needed: the FMU binary is not loaded, so FMUs of any platform can be exported
ModelDescription modelDescription;
try
{
  modelDescription = ModelLoader.LoadModelFromFmu(FmuPath, FmiLogCallback);
}
catch
{
  if (Environment.ExitCode == ExitCodes.Success)
  {
    Environment.ExitCode = ExitCodes.FailedToReadModelDescription;
  }
  throw;
}

    // Check GenerationTool before parsing, if Vector vVIRTUALtarget FMU, copy-paste it instead of parsing
    if (modelDescription.GenerationTool == ModelDescription.GenerationTools.Vector_vVIRTUALtarget)
    {
      using var fmuArchive = ZipFile.OpenRead(FmuPath);
      var vcdlEntry = FindVcdlEntry(fmuArchive);
      if (vcdlEntry != null)
      {
        Console.WriteLine("The imported FMU has been identified as a Vector vVIRTUALtarget FMU. The .vCDL file included in this FMU will be extracted to the defined output path.");
        if (useClockPubSubElements)
        {
          Console.WriteLine("The \"--use-clock-pub-sub-elements\" option will be ignored.");
        }

        // Copy FMU vCDL file instead of parsing
        vcdlEntry.ExtractToFile(VcdlPath, overwrite: true);
        return;
      }
      Console.WriteLine("The provided Vector vVIRTUALtarget FMU does not contain a .vCDL file. A .vCDL file will be exported to the defined output path.");
    }

    // Proceed with parsing
    AddVcdlHeader(commonTextSb, modelDescription.CoSimulation.ModelIdentifier);

    switch (fmiVersion)
    {
      case FmiVersions.Fmi2:
        ParseFmi2(modelDescription, interfaceSb, objectsSb);
        break;
      case FmiVersions.Fmi3:
        ParseFmi3(modelDescription, interfaceSb, objectsSb, useClockPubSubElements);
        break;
    }

    commonTextSb.Append(interfaceSb.ToString());
    commonTextSb.Append(objectsSb.ToString());

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
      // assume scalar
      if (vValue.Dimensions != null && vValue.Dimensions.Length > 0)
      {
        throw new NotSupportedException(
          $"FMI 2.0.x does not support arrays. Check variable '{vValue.Name}'.");
      }

      var v = new VcdlVariable()
      {
        Name = vValue.Name,
        Type = CreateVariableType(BuildVariableType(vValue))
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
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Parameter:
        case Variable.Causalities.StructuralParameter:
        case Variable.Causalities.Local:
          // ignore - independent variables (e.g. 'time') and parameters are not communication signals; the communication interface generator omits them too.
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }
    }

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

  private void ParseFmi3(ModelDescription modelDescription, StringBuilder interfaceSb, StringBuilder objectsSb, bool useClockPubSubElements)
  {
    var vcdlProviderVariables = new HashSet<VcdlVariable>();
    var vcdlConsumerVariables = new HashSet<VcdlVariable>();

    AddEnumerationDefinitions(modelDescription, interfaceSb);

    foreach (var arrayVar in modelDescription.ArrayVariables.Values)
    {
      arrayVar.InitializeArrayLength(modelDescription.Variables);
    }

    foreach (var variable in modelDescription.Variables)
    {
      if (variable.Value.VariableType is VariableTypes.TriggeredClock && !useClockPubSubElements)
      {
        // TriggeredClocks are not bound to a pubSub topic when useClockPubSubElements is disabled
        continue;
      }

      var vValue = variable.Value;
      // Note that the direction is reversed compared to the model description
      // input  -> provided // CANoe _provides_ the _input_ value for an FMU
      // output -> consumed // CANoe _consumes_ the _output_ value of an FMU
      var v = new VcdlVariable()
      {
        Name = vValue.Name,
        Type = CreateVariableType(BuildVariableType(vValue))
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
        case Variable.Causalities.CalculatedParameter:
        case Variable.Causalities.Parameter:
        case Variable.Causalities.StructuralParameter:
        case Variable.Causalities.Local:
          // ignore - independent variables (e.g. 'time') and parameters are not communication signals; the communication interface generator omits them too.
          continue;
        default:
          throw new InvalidDataException($"The variable '{vValue.Name}' has an unknown causality.");
      }
    }

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

  // Builds the type model for a variable: a leaf type wrapped in one list per array dimension.
  // The resulting OptionalType is rendered by the shared BaseExporter.CreateVariableType, so the FMU
  // export path and the communication-interface export path use the exact same vCDL type renderer.
  private OptionalType BuildVariableType(Variable variable)
  {
    var leafTypeName = variable.VariableType is VariableTypes.EnumFmi2 or VariableTypes.EnumFmi3
                         ? variable.TypeDefinition!.Name
                         : GetVarTypeString(variable.VariableType);

    OptionalType type = new OptionalType(isOptional: false, isList: false, customTypeName: leafTypeName);

    // Wrap the type in one list per array dimension (e.g. a 2-dimensional array becomes list<list<double>>).
    var dimensionCount = variable.Dimensions?.Length ?? 0;
    for (var i = 0; i < dimensionCount; i++)
    {
      type = new OptionalType(isOptional: false, isList: true, optionalType: type);
    }

    return type;
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
    // FMI 3.0 triggered clocks can be handled as bool vars
    typeName = typeName.Replace("triggeredclock", "bool");
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
