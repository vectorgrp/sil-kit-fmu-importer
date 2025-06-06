// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi.Exceptions;

namespace Fmi.FmiModel.Internal;

public class ModelDescription
{
  public enum VariableNamingConventions
  {
    Flat,
    Structured
  }
  public enum GenerationTools
  {
    Vector_vVIRTUALtarget,
    Other,
    Unset
  }

  // Former attributes
  public string ModelName { get; set; }
  public string Description { get; set; }
  public string InstantiationToken { get; set; }
  public GenerationTools GenerationTool { get; set; }
  public string FmiVersion { get; set; }
  public string Version { get; set; }
  public VariableNamingConventions VariableNamingConvention { get; set; }


  // Former nodes
  public CoSimulation CoSimulation { get; set; }
  public DefaultExperiment DefaultExperiment { get; set; }

  public Dictionary<uint /* ValueReference */, Variable> Variables;

  /// <summary>
  ///   This contains the subset of Variables which have 'Dimension' elements.
  /// </summary>
  public Dictionary<uint /* ValueReference */, Variable> ArrayVariables;

  public Dictionary<string, uint> NameToValueReference { get; set; }

  public Dictionary<string /*unitName*/, UnitDefinition> UnitDefinitions;

  public Dictionary<string /*typeName*/, TypeDefinition> TypeDefinitions;

  public ModelStructure ModelStructure { get; set; }

  public ModelDescription(Fmi3.fmiModelDescription input, Action<LogSeverity, string> logCallback)
  {
    // init of local fields & properties
    UnitDefinitions = new Dictionary<string, UnitDefinition>();
    TypeDefinitions = new Dictionary<string, TypeDefinition>();
    Variables = new Dictionary<uint, Variable>();
    ArrayVariables = new Dictionary<uint, Variable>();
    NameToValueReference = new Dictionary<string, uint>();

    // Attribute init
    ModelName = input.modelName;
    Description = input.description;
    InstantiationToken = input.instantiationToken.Normalize();
    GenerationTool = string.IsNullOrEmpty(input.generationTool)
      ? GenerationTools.Unset
      : input.generationTool.Normalize().Contains("Vector vVIRTUALtarget")
        ? GenerationTools.Vector_vVIRTUALtarget
        : GenerationTools.Other;
    FmiVersion = input.fmiVersion;
    Version = input.version;
    VariableNamingConvention =
      input.variableNamingConvention == Fmi3.fmiModelDescriptionVariableNamingConvention.structured
        ? VariableNamingConventions.Structured
        : VariableNamingConventions.Flat;

    // Node init
    CoSimulation = new CoSimulation(input.CoSimulation);

    DefaultExperiment = new DefaultExperiment(input.DefaultExperiment);
    if (input.UnitDefinitions != null)
    {
      InitUnitMap(input.UnitDefinitions);
    }

    if (input.TypeDefinitions != null)
    {
      InitTypeDefMap(input.TypeDefinitions);
    }

    if (input.ModelVariables != null)
    {
      InitVariableMap(input.ModelVariables, logCallback);
    }

    ModelStructure = new ModelStructure(input.ModelStructure);
  }

  public ModelDescription(Fmi2.fmiModelDescription input, Action<LogSeverity, string> logCallback)
  {
    // init of local fields & properties
    UnitDefinitions = new Dictionary<string, UnitDefinition>();
    TypeDefinitions = new Dictionary<string, TypeDefinition>();
    Variables = new Dictionary<uint, Variable>();
    ArrayVariables = new Dictionary<uint, Variable>();
    NameToValueReference = new Dictionary<string, uint>();

    // Attribute init
    ModelName = input.modelName;
    Description = input.description;
    InstantiationToken = input.guid.Normalize();
    FmiVersion = input.fmiVersion;
    Version = input.version;
    VariableNamingConvention =
      input.variableNamingConvention == Fmi2.fmiModelDescriptionVariableNamingConvention.structured
        ? VariableNamingConventions.Structured
        : VariableNamingConventions.Flat;

    // Node init
    if (input.CoSimulation.Length < 1)
    {
      throw new ModelDescriptionException("The model description does not provide a CoSimulation description.");
    }

    CoSimulation = new CoSimulation(input.CoSimulation[0]);
    DefaultExperiment = new DefaultExperiment(input.DefaultExperiment);
    if (input.UnitDefinitions != null)
    {
      InitUnitMap(input.UnitDefinitions);
    }

    if (input.TypeDefinitions != null)
    {
      InitTypeDefMap(input.TypeDefinitions);
    }

    if (input.ModelVariables != null)
    {
      InitVariableMap(input.ModelVariables, logCallback);
    }

    ModelStructure = new ModelStructure(input.ModelStructure, input.ModelVariables!);
  }

  private void InitTypeDefMap(Fmi3.fmiModelDescriptionTypeDefinitions input)
  {
    foreach (var typeDefinitionBase in input.Items)
    {
      if (typeDefinitionBase == null)
      {
        continue;
      }

      if (typeDefinitionBase is Fmi3.fmiModelDescriptionTypeDefinitionsFloat64Type typeDefFloat64)
      {
        if (!IsUnitInMap(typeDefFloat64.unit))
        {
          throw new ModelDescriptionException($"The type definition 'Float64' in the model description has a unit " +
            $"'{typeDefFloat64.unit}' that does not match.");
        }

        var typeDef = new TypeDefinition()
        {
          Name = typeDefinitionBase.name,
          Unit = UnitDefinitions[typeDefFloat64.unit]
        };
        TypeDefinitions.Add(typeDef.Name, typeDef);
      }
      else if (typeDefinitionBase is Fmi3.fmiModelDescriptionTypeDefinitionsFloat32Type typeDefFloat32)
      {
        if (!IsUnitInMap(typeDefFloat32.unit))
        {
          throw new ModelDescriptionException($"The type definition 'Float32' in the model description has a unit " +
            $"'{typeDefFloat32.unit}' that does not match.");
        }

        var typeDef = new TypeDefinition
        {
          Name = typeDefinitionBase.name,
          Unit = UnitDefinitions[typeDefFloat32.unit]
        };
        TypeDefinitions.Add(typeDef.Name, typeDef);
      }
      else if (typeDefinitionBase is Fmi3.fmiModelDescriptionTypeDefinitionsEnumerationType typeDefEnum)
      {
        var typeDef = new TypeDefinition
        {
          Name = typeDefinitionBase.name,
          EnumerationValues = new Tuple<string, long>[typeDefEnum.Item.Length]
        };

        for (var i = 0; i < typeDefEnum.Item.Length; i++)
        {
          var enumValue = typeDefEnum.Item[i];
          typeDef.EnumerationValues[i] = new Tuple<string, long>(enumValue.name, enumValue.value);
        }

        TypeDefinitions.Add(typeDef.Name, typeDef);
      }
    }
  }

  private void InitTypeDefMap(Fmi2.fmiModelDescriptionTypeDefinitions input)
  {
    foreach (var fmi2SimpleType in input.SimpleType)
    {
      if (fmi2SimpleType == null)
      {
        continue;
      }

      if (fmi2SimpleType.Item is Fmi2.fmi2SimpleTypeReal typeDefReal)
      {
        if (!IsUnitInMap(typeDefReal.unit))
        {
          throw new ModelDescriptionException($"The type definition 'Real' in the model description has a unit " +
            $"'{typeDefReal.unit}' that does not match.");
        }

        var typeDef = new TypeDefinition
        {
          Name = fmi2SimpleType.name,
          Unit = UnitDefinitions[typeDefReal.unit]
        };
        TypeDefinitions.Add(typeDef.Name, typeDef);
      }
      else if (fmi2SimpleType.Item is Fmi2.fmi2SimpleTypeEnumeration typeDefEnum)
      {
        var typeDef = new TypeDefinition
        {
          Name = fmi2SimpleType.name,
          EnumerationValues = new Tuple<string, long>[typeDefEnum.Item.Length]
        };

        for (var i = 0; i < typeDefEnum.Item.Length; i++)
        {
          var enumValue = typeDefEnum.Item[i];
          typeDef.EnumerationValues[i] = new Tuple<string, long>(enumValue.name, enumValue.value);
        }

        TypeDefinitions.Add(typeDef.Name, typeDef);
      }
    }
  }

  private void InitUnitMap(Fmi3.fmiModelDescriptionUnitDefinitions input)
  {
    if (input.Unit == null)
    {
      return;
    }

    foreach (var fmi3Unit in input.Unit)
    {
      var unitDefinition = new UnitDefinition
      {
        Name = fmi3Unit.name
      };

      if (fmi3Unit.BaseUnit != null)
      {
        unitDefinition.Offset = fmi3Unit.BaseUnit.offset;
        unitDefinition.Factor = fmi3Unit.BaseUnit.factor;
      }

      UnitDefinitions.Add(unitDefinition.Name, unitDefinition);
    }
  }

  private void InitUnitMap(Fmi2.fmiModelDescriptionUnitDefinitions input)
  {
    if (input.Unit == null)
    {
      return;
    }

    foreach (var fmi2Unit in input.Unit)
    {
      var unitDefinition = new UnitDefinition
      {
        Name = fmi2Unit.name
      };

      if (fmi2Unit.BaseUnit != null)
      {
        unitDefinition.Offset = fmi2Unit.BaseUnit.offset;
        unitDefinition.Factor = fmi2Unit.BaseUnit.factor;
      }

      UnitDefinitions.Add(unitDefinition.Name, unitDefinition);
    }
  }

  private bool IsUnitInMap(string unitName)
  {
    return UnitDefinitions.ContainsKey(unitName);
  }

  private void InitVariableMap(Fmi3.fmiModelDescriptionModelVariables input, Action<LogSeverity, string> logCallback)
  {
    if (input.Items == null)
    {
      return;
    }

    foreach (var fmiModelDescriptionModelVariable in input.Items)
    {
      var v = new Variable(fmiModelDescriptionModelVariable, TypeDefinitions, logCallback);
      if (v.VariableType == VariableTypes.TimeBasedClock)
      {
        continue;
      }

      var result = Variables.TryAdd(v.ValueReference, v);
      if (!result)
      {
        logCallback.Invoke(
          LogSeverity.Warning,
          $"Variable {v.Name} has the same value reference ({v.ValueReference}) than a previous variable entry. Discarding duplicate.");
      }

      if (!v.IsScalar)
      {
        ArrayVariables.Add(v.ValueReference, v);
      }

      result = NameToValueReference.TryAdd(v.Name, v.ValueReference);
      if (!result)
      {
        throw new ModelDescriptionException(
          $"Failed to parse model description: multiple variables have the same name. Exception thrown when parsing " +
          $"{v.Name} with the following value reference: {v.ValueReference}");
      }
    }
  }

  private void InitVariableMap(Fmi2.fmiModelDescriptionModelVariables input, Action<LogSeverity, string> logCallback)
  {
    if (input.ScalarVariable == null)
    {
      return;
    }

    foreach (var fmiModelDescriptionModelVariable in input.ScalarVariable)
    {
      var v = new Variable(fmiModelDescriptionModelVariable, TypeDefinitions);
      var result = Variables.TryAdd(v.ValueReference, v);
      if (!result)
      {
        logCallback.Invoke(
          LogSeverity.Warning,
          $"Variable {v.Name} has the same value reference ({v.ValueReference}) than a previous variable entry. Discarding duplicate.");
      }

      result = NameToValueReference.TryAdd(v.Name, v.ValueReference);
      if (!result)
      {
        throw new ModelDescriptionException(
          $"Failed to parse model description: multiple variables have the same name. Exception thrown when parsing " +
          $"{v.Name} with the following value reference: {v.ValueReference}");
      }
    }
  }

  public override string ToString()
  {
    var stringBuilder = new StringBuilder();
    stringBuilder.AppendLine("ModelName: " + ModelName);
    stringBuilder.AppendLine("FMI version: " + FmiVersion);
    stringBuilder.AppendLine("Description: " + Description);

    stringBuilder.AppendLine();
    foreach (var variable in Variables.Values)
    {
      stringBuilder.AppendLine(variable.ToString());
    }

    return stringBuilder.ToString();
  }
}
