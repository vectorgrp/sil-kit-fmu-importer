// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
using System.Text;
using Fmi;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using FmuImporter.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace VcdlExporter;

public class ModelDescriptionException : Exception
{
  public ModelDescriptionException(string message, Exception innerException) : base(message, innerException)
  {
  }
}

public class FmuExporter : BaseExporter
{
  public string FmuPath { get; }
  public string VcdlPath { get; }

  private readonly ILogger _logger;
  private readonly IdentifierValidator _identifierValidator = new(
    "model description",
    "Names containing '.' follow the structured naming convention: generate a communication " +
    "interface description with the CommInterfaceExporter and export it with the " +
    "'communicationInterface' command instead.");

  public FmuExporter(string fmuPath, string vcdlPath, ILogger logger)
  {
    FmuPath = fmuPath;
    VcdlPath = vcdlPath;
    _logger = logger;
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

    if (!File.Exists(FmuPath))
    {
      throw new FileNotFoundException($"The FMU '{FmuPath}' does not exist.", FmuPath);
    }

    var fmiVersion = ReadModelDescription(() => ModelLoader.FindFmiVersion(FmuPath));
    if (fmiVersion is not (FmiVersions.Fmi2 or FmiVersions.Fmi3))
    {
      throw new InvalidDataException("The FMU uses an unsupported FMU version.");
    }

    _logger.LogDebug("Detected FMI version: {FmiVersion}", fmiVersion);

    // Only the model description is needed: the FMU binary is not loaded, so FMUs of any platform can be exported
    var modelDescription = ReadModelDescription(() => ModelLoader.LoadModelFromFmu(FmuPath, FmiLogCallback));

    // Check GenerationTool before parsing, if Vector vVIRTUALtarget FMU, copy-paste it instead of parsing
    if (modelDescription.GenerationTool == ModelDescription.GenerationTools.Vector_vVIRTUALtarget)
    {
      using var fmuArchive = ZipFile.OpenRead(FmuPath);
      var vcdlEntry = FindVcdlEntry(fmuArchive);
      if (vcdlEntry != null)
      {
        _logger.LogInformation(
          "The imported FMU has been identified as a Vector vVIRTUALtarget FMU. " +
          "The .vCDL file included in this FMU ('{FmuVcdlPath}') will be copied to '{VcdlPath}'.",
          vcdlEntry.FullName,
          VcdlPath);
        if (useClockPubSubElements)
        {
          _logger.LogWarning("The \"--use-clock-pub-sub-elements\" option is ignored for Vector vVIRTUALtarget FMUs.");
        }

        // Copy FMU vCDL file instead of parsing
        vcdlEntry.ExtractToFile(VcdlPath, overwrite: true);
        return;
      }
      _logger.LogWarning(
        "The provided Vector vVIRTUALtarget FMU does not contain a .vCDL file. " +
        "The vCDL will be generated from the model description instead.");
    }

    // Proceed with parsing
    _identifierValidator.Check(modelDescription.CoSimulation.ModelIdentifier, "Model identifier");
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

    // Fail before writing anything, so that no invalid vCDL file is left behind
    _identifierValidator.ThrowIfInvalid(_logger);

    _logger.LogInformation("Writing vCDL to '{VcdlPath}'.", VcdlPath);
    File.WriteAllText(VcdlPath, commonTextSb.ToString());
  }

  // Wraps any failure while reading the model description into a ModelDescriptionException
  private T ReadModelDescription<T>(Func<T> read)
  {
    try
    {
      return read();
    }
    catch (Exception e)
    {
      throw new ModelDescriptionException($"Failed to read the model description of '{FmuPath}': {e.Message}", e);
    }
  }

  private void FmiLogCallback(LogSeverity severity, string message)
  {
    var logLevel = severity switch
    {
      LogSeverity.Error => LogLevel.Error,
      LogSeverity.Warning => LogLevel.Warning,
      LogSeverity.Information => LogLevel.Information,
      LogSeverity.Debug => LogLevel.Debug,
      LogSeverity.Trace => LogLevel.Trace,
      _ => LogLevel.Information
    };
    _logger.Log(logLevel, "{Message}", message);
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

      _identifierValidator.Check(vValue.Name, $"{vValue.Causality} variable");
    }

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    RemoveProvidedVariables(vcdlConsumerVariables, vcdlProviderVariables);

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

      _identifierValidator.Check(vValue.Name, $"{vValue.Causality} variable");
    }

    // Remove all variables that are already part of the provider list (feedback loops can only be observed)
    RemoveProvidedVariables(vcdlConsumerVariables, vcdlProviderVariables);

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
                         // Same renaming as in AddEnumerationDefinitions, so the variable references the written enum
                         ? variable.TypeDefinition!.Name.Replace(' ', '_')
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
    // Only the enumeration types used by exported variables are needed in the vCDL,
    // so unused types are skipped (and not validated), like unused variables
    var usedTypeNames = modelDescription.Variables.Values
      .Where(v => v.Causality is Variable.Causalities.Input or Variable.Causalities.Output)
      .Select(v => v.TypeDefinition?.Name)
      .OfType<string>()
      .ToHashSet();

    // Spaces are replaced by '_', so distinct types (e.g. 'A B' and 'A_B') can end up with the same name
    // Such duplicates are reported as invalid identifiers, so the export fails
    var originalTypeNames = new Dictionary<string, string>();
    foreach (var typedefinition in modelDescription.TypeDefinitions)
    {
      if (typedefinition.Value.EnumerationValues != null && usedTypeNames.Contains(typedefinition.Key))
      {
        var typeName = typedefinition.Value.Name.Replace(' ', '_');
        if (typeName != typedefinition.Value.Name)
        {
          _logger.LogInformation(
            "Enumeration type '{OriginalName}' has been renamed to '{TypeName}'.",
            typedefinition.Value.Name,
            typeName);
        }

        _identifierValidator.Check(typeName, "Enumeration type");
        if (!originalTypeNames.TryAdd(typeName, typedefinition.Value.Name))
        {
          _identifierValidator.ReportDuplicate(
            typeName, originalTypeNames[typeName], typedefinition.Value.Name, "Enumeration type");
        }

        sb.AppendLine($"  enum {typeName} : int64");
        sb.AppendLine("  {");

        var originalNames = new Dictionary<string, string>();
        foreach (var val in typedefinition.Value.EnumerationValues)
        {
          var itemName = val.Item1.Replace(' ', '_');
          if (itemName != val.Item1)
          {
            _logger.LogInformation(
              "Item '{OriginalName}' of enumeration type '{TypeName}' has been renamed to '{ItemName}'.",
              val.Item1,
              typedefinition.Value.Name,
              itemName);
          }

          var itemKind = $"Item of enumeration type '{typedefinition.Value.Name}'";
          _identifierValidator.Check(itemName, itemKind);
          if (!originalNames.TryAdd(itemName, val.Item1))
          {
            _identifierValidator.ReportDuplicate(itemName, originalNames[itemName], val.Item1, itemKind);
          }

          sb.AppendLine($"    {itemName} = {val.Item2},");
        }

        sb.AppendLine("  }\n");
      }
    }
  }
}
