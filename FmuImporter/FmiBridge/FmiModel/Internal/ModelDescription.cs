// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi.Exceptions;

namespace Fmi.FmiModel.Internal;

public class ModelDescription
{
  // Former attributes
  public string ModelName { get; set; }
  public string Description { get; set; }
  public string InstantiationToken { get; set; }
  public string FmiVersion { get; set; }
  public string Version { get; set; }


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

  public ModelDescription(Fmi3.fmiModelDescription input)
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
    FmiVersion = input.fmiVersion;
    Version = input.version;

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
      InitVariableMap(input.ModelVariables);
    }

    ModelStructure = new ModelStructure(input.ModelStructure);
  }

  public ModelDescription(Fmi2.fmiModelDescription input)
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
      InitVariableMap(input.ModelVariables);
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
          throw new ModelDescriptionException("The model description has a type definition with no matching unit.");
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
          throw new ModelDescriptionException("The model description has a type definition with no matching unit.");
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
          throw new ModelDescriptionException("The model description has a type definition with no matching unit.");
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
        Name = fmi3Unit.name,
        Offset = fmi3Unit.BaseUnit.offset,
        Factor = fmi3Unit.BaseUnit.factor
      };

      UnitDefinitions.Add(unitDefinition.Name, unitDefinition);
    }
  }

  private void InitUnitMap(Fmi2.fmiModelDescriptionUnitDefinitions input)
  {
    if (input.Unit == null)
    {
      return;
    }

    foreach (var fmi3Unit in input.Unit)
    {
      var unitDefinition = new UnitDefinition
      {
        Name = fmi3Unit.name,
        Offset = fmi3Unit.BaseUnit.offset,
        Factor = fmi3Unit.BaseUnit.factor
      };

      UnitDefinitions.Add(unitDefinition.Name, unitDefinition);
    }
  }

  private bool IsUnitInMap(string unitName)
  {
    return UnitDefinitions.ContainsKey(unitName);
  }

  private void InitVariableMap(Fmi3.fmiModelDescriptionModelVariables input)
  {
    if (input.Items == null)
    {
      return;
    }

    foreach (var fmiModelDescriptionModelVariable in input.Items)
    {
      var v = new Variable(fmiModelDescriptionModelVariable, TypeDefinitions);
      var result = Variables.TryAdd(v.ValueReference, v);
      if (!result)
      {
        throw new ArgumentException(
          "Failed to parse model description: multiple variables have the same value reference.");
      }

      if (!v.IsScalar)
      {
        ArrayVariables.Add(v.ValueReference, v);
      }

      result = NameToValueReference.TryAdd(v.Name, v.ValueReference);
      if (!result)
      {
        throw new ModelDescriptionException(
          "Failed to parse model description: multiple variables have the same name.");
      }
    }
  }

  private void InitVariableMap(Fmi2.fmiModelDescriptionModelVariables input)
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
        throw new ModelDescriptionException(
          "Failed to parse model description: multiple variables have the same valueReference.");
      }

      result = NameToValueReference.TryAdd(v.Name, v.ValueReference);
      if (!result)
      {
        throw new ModelDescriptionException(
          "Failed to parse model description: multiple variables have the same name.");
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
