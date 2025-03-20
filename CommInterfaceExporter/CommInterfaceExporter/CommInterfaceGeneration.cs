// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi;
using Fmi.Binding;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;
using Fmi.Supplements;

namespace CommInterfaceGenerator;

internal class CommInterfaceGeneration
{
  public static string GenerateFromFile(string fmuPath)
  {
    var FmiVersion = ModelLoader.FindFmiVersion(fmuPath);
    return GenerateFrom(BindingFactory.CreateBinding(FmiVersion, fmuPath, LogCallback).ModelDescription);
  }

  private static void LogCallback(LogSeverity arg1, string arg2)
  {
    Console.WriteLine(arg2);
  }

  private static string GenerateFrom(ModelDescription modelDescription)
  {
    var result = new StringBuilder();

    result.AppendLine("Version: 1");
    result.AppendLine();

    GenerateEnumDefinitions(modelDescription, result);

    GenerateStructDefinitionsPubsAndSubs(modelDescription, result);

    return result.ToString();
  }

  private static string GenerateStructNameFromPath(string radical)
  {
    return radical + "_struct";
  }

  private static string StringOf(VariableTypes varType, TypeDefinition? valueTypeDefinition)
  {
    switch (varType)
    {
      case VariableTypes.Float32:
        return "float";
      case VariableTypes.Float64:
        return "double";
      case VariableTypes.Int8:
        return "int8";
      case VariableTypes.Int16:
        return "int16";
      case VariableTypes.Int32:
        return "int32";
      case VariableTypes.Int64:
        return "int64";
      case VariableTypes.UInt8:
        return "uint8";
      case VariableTypes.UInt16:
        return "uint16";
      case VariableTypes.UInt32:
        return "uint32";
      case VariableTypes.UInt64:
        return "uint64";
      case VariableTypes.Boolean:
      case VariableTypes.TriggeredClock:
        return "bool";
      case VariableTypes.String:
        return "string";
      case VariableTypes.Binary:
        return "byte[]";
      case VariableTypes.EnumFmi2:
      case VariableTypes.EnumFmi3:
        if (valueTypeDefinition is not null)
        {
          return valueTypeDefinition.Name;
        }
        else
        {
          goto default;
        }
      case VariableTypes.Undefined:
      default:
        throw new NotImplementedException();
    }
  }

  private static void GenerateStructDefinitionsPubsAndSubs(ModelDescription modelDescription, StringBuilder result)
  {
    var publishers = new StringBuilder();
    var subscribers = new StringBuilder();
    // This contains the future content of "StructDefinitions:". Key is the path with instance name ('a.b.c')
    var structsDictionary = new Dictionary<string, Dictionary<string, string>>();
    // This contains the *actual* name (modified with GenerateStructNameFromPath) of structs. Same keys as 'structsDictionnary'
    var generatedStructName = new Dictionary<string, string>();

    foreach (var variable in modelDescription.Variables)
    {
      if (variable.Value.Causality is Variable.Causalities.Local 
          or Variable.Causalities.CalculatedParameter)
      {
        // Those variable types are not input or output, and so should be ignored
        continue;
      }

      var parsedName = StructuredVariableParser.Parse(variable.Value.Name);
      var topicName = parsedName.RootName;
      var varType = StringOf(variable.Value.VariableType, variable.Value.TypeDefinition);
      varType = variable.Value.IsScalar
                  ? varType
                  : ("List<" + varType + ">");

      var pubSubSb = variable.Value.Causality switch
      {
        Variable.Causalities.Input => subscribers,
        _ => publishers
      };

      if (pubSubSb.Length == 0)
      {
        // The ternary below, while reevaluating the same case as a previous branch, is done only once per string builder.
        pubSubSb.AppendLine(variable.Value.Causality is Variable.Causalities.Input ? "Subscribers:" : "Publishers:");
      }

      if (parsedName.Path.Count < 2)
      {
        pubSubSb.AppendLine("  - " + topicName + ": " + varType);
      }
      else
      {
        if (!generatedStructName.ContainsKey(topicName))
        {
          var pubSubTypeName = GenerateStructNameFromPath(topicName);
          generatedStructName.Add(topicName, pubSubTypeName);
          structsDictionary.Add(topicName, new Dictionary<string, string>());
          // Only output the pub/sub once
          pubSubSb.AppendLine("  - " + topicName + ": " + pubSubTypeName);
        }
      }

      if (parsedName.Path.Count < 2)
      {
        continue;
      }

      // This variable will grow into the innermost structure path
      var parentPath = parsedName.RootName;

      // Handling intermediate structs as we grow parentPath
      for (var i = 1; i < parsedName.Path.Count - 1; i++)
      {
        var pathElement = parsedName.Path[i];
        var currentPath = parentPath + '.' + pathElement;

        var intermediateStructNameAsMember = pathElement;

        if (!generatedStructName.TryGetValue(currentPath, out var intermediateStructName))
        {
          intermediateStructName = GenerateStructNameFromPath(currentPath);
          generatedStructName.Add(currentPath, intermediateStructName);
          structsDictionary.Add(currentPath, new Dictionary<string, string>());
        }

        // This will never fail because it's populated in advance.
        // See lookup of intermediateStructName and pubSubTypeName above.
        var intermediateStructParentMembers = structsDictionary[parentPath];

        intermediateStructParentMembers.TryAdd(intermediateStructNameAsMember, intermediateStructName);
        parentPath = currentPath;
      }

      var structElemName = parsedName.Path.Last();
      var structElemType = varType;

      // This will never fail because it's populated in advance.
      // See lookup of intermediateStructName and pubSubTypeName above.
      structsDictionary[parentPath].Add(structElemName, structElemType);
    }

    var structDefinitions = new StringBuilder();

    if (structsDictionary.Count > 0)
    {
      structDefinitions.AppendLine("StructDefinitions:");
    }

    foreach (var structDef in structsDictionary)
    {
      structDefinitions.AppendLine("  - Name: " + generatedStructName[structDef.Key]);
      structDefinitions.AppendLine("    Members:");
      foreach (var member in structDef.Value)
      {
        structDefinitions.AppendLine("      - " + member.Key + ": " + member.Value);
      }
    }

    if (structDefinitions.Length > 0)
    {
      result.AppendLine(structDefinitions.ToString());
    }

    if (publishers.Length > 0)
    {
      result.AppendLine(publishers.ToString());
    }

    if (subscribers.Length > 0)
    {
      result.AppendLine(subscribers.ToString());
    }
  }

  private static void GenerateEnumDefinitions(ModelDescription modelDescription, StringBuilder result)
  {
    var enumsPrinted = false;
    foreach (var typeDef in modelDescription.TypeDefinitions)
    {
      if (typeDef.Value.EnumerationValues is not null && typeDef.Value.EnumerationValues.Length > 0)
      {
        if (!enumsPrinted)
        {
          result.AppendLine("EnumDefinitions :");
          enumsPrinted = true;
        }

        result.AppendLine("  - Name: " + typeDef.Value.Name);
        result.AppendLine("    Items: ");
        foreach (var eValue in typeDef.Value.EnumerationValues)
        {
          result.AppendLine("      - " + eValue.Item1 + ": " + eValue.Item2);
        }
      }
    }

    if (enumsPrinted)
    {
      result.AppendLine();
    }
  }
}
