// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi.Exceptions;
using Fmi2;
using Fmi3;

namespace Fmi.FmiModel.Internal;

public class Variable
{
  private readonly Fmi3.fmi3AbstractVariable originalVariable = null!;

  public enum Causalities
  {
    Parameter,
    CalculatedParameter,
    Input,
    Output,
    Local, // Not relevant in Co-Sim; May be stored for debugging
    Independent,
    StructuralParameter
  }

  public enum Variabilities
  {
    Unset,
    Constant,
    Fixed,
    Tunable,
    Discrete,
    Continuous // default
  }

  public enum InitialValues
  {
    Unset,
    Exact,
    Approx,
    Calculated
  }

  public string Name { get; set; }
  public uint ValueReference { get; set; }
  public string Description { get; set; }
  public Causalities Causality { get; set; }
  public Variabilities Variability { get; set; }
  public InitialValues InitialValue { get; set; }

  public VariableTypes VariableType { get; private set; }

  public object[]? Start { get; set; }

  private ulong[]? dimensions;

  public ulong[]? Dimensions
  {
    get
    {
      return dimensions;
    }
    set
    {
      dimensions = value;
      FlattenedArrayLength = 1;
      if (value == null || value.Length == 0)
      {
        return;
      }

      foreach (var dim in value)
      {
        FlattenedArrayLength *= dim;
      }
    }
  }

  public TypeDefinition? TypeDefinition { get; set; }

  public ulong FlattenedArrayLength { get; private set; } = 1;

  public Variable(fmi3AbstractVariable input, Dictionary<string, TypeDefinition> typeDefinitions)
  {
    originalVariable = input;

    Name = input.name;
    ValueReference = input.valueReference;
    Description = input.description;

    switch (input)
    {
      case Fmi3.fmi3Float32 inputVar:
        VariableType = VariableTypes.Float32;
        if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
        {
          TypeDefinition = typeDefinitions[inputVar.declaredType];
        }

        break;
      case Fmi3.fmi3Float64 inputVar:
        VariableType = VariableTypes.Float64;
        if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
        {
          TypeDefinition = typeDefinitions[inputVar.declaredType];
        }

        break;
      case fmi3Int8:
        VariableType = VariableTypes.Int8;
        break;
      case fmi3UInt8:
        VariableType = VariableTypes.UInt8;
        break;
      case fmi3Int16:
        VariableType = VariableTypes.Int16;
        break;
      case fmi3UInt16:
        VariableType = VariableTypes.UInt16;
        break;
      case fmi3Int32:
        VariableType = VariableTypes.Int32;
        break;
      case fmi3UInt32:
        VariableType = VariableTypes.UInt32;
        break;
      case fmi3Int64:
        VariableType = VariableTypes.Int64;
        break;
      case fmi3UInt64:
        VariableType = VariableTypes.UInt64;
        break;
      case fmi3Boolean:
        VariableType = VariableTypes.Boolean;
        break;
      case fmi3String:
        VariableType = VariableTypes.String;
        break;
      case Fmi3.fmi3Binary:
        VariableType = VariableTypes.Binary;
        break;
      case fmi3Enumeration:
        VariableType = VariableTypes.EnumFmi3;
        break;
      default:
        throw new InvalidDataException("The FMI 3 datatype is unknown.");
    }

    switch (input.causality)
    {
      case fmi3AbstractVariableCausality.parameter:
        Causality = Causalities.Parameter;
        break;
      case fmi3AbstractVariableCausality.calculatedParameter:
        Causality = Causalities.CalculatedParameter;
        break;
      case fmi3AbstractVariableCausality.input:
        Causality = Causalities.Input;
        break;
      case fmi3AbstractVariableCausality.output:
        Causality = Causalities.Output;
        break;
      case fmi3AbstractVariableCausality.local:
        Causality = Causalities.Local;
        break;
      case fmi3AbstractVariableCausality.independent:
        Causality = Causalities.Independent;
        break;
      case fmi3AbstractVariableCausality.structuralParameter:
        Causality = Causalities.StructuralParameter;
        break;
      default:
        throw new InvalidDataException($"The variable '{input.name}' has an unknown causality.");
    }

    if (input.variabilitySpecified)
    {
      switch (input.variability)
      {
        case fmi3AbstractVariableVariability.constant:
          Variability = Variabilities.Constant;
          break;
        case fmi3AbstractVariableVariability.@fixed:
          Variability = Variabilities.Fixed;
          break;
        case fmi3AbstractVariableVariability.tunable:
          Variability = Variabilities.Tunable;
          break;
        case fmi3AbstractVariableVariability.discrete:
          Variability = Variabilities.Discrete;
          break;
        case fmi3AbstractVariableVariability.continuous:
          Variability = Variabilities.Continuous;
          break;
        default:
          throw new InvalidDataException($"The variable '{input.name}' has an unknown variability.");
      }
    }
    else
    {
      Variability = Variabilities.Continuous; // default
    }

    var propInfo = input.GetType().GetProperty("start");
    if (propInfo != null)
    {
      var res = propInfo.GetValue(input);
      if (res != null)
      {
        int arrLength;
        if (res is Array array)
        {
          arrLength = array.Length;
          Start = new object[arrLength];
          for (var i = 0; i < arrLength; i++)
          {
            Start[i] = array.GetValue(i) ?? throw new InvalidOperationException();
          }
        }
        else if (res is List<byte> byteList)
        {
          // special case
          Start = byteList.Select(v => (object)v).ToArray();
        }
        else
        {
          Start = new[] { res };
        }
      }
    }
  }

  public Variable(fmi2ScalarVariable input, Dictionary<string, TypeDefinition> typeDefinitions)
  {
    Name = input.name;
    ValueReference = input.valueReference;
    Description = input.description;

    switch (input.Item)
    {
      case fmi2ScalarVariableInteger:
        VariableType = VariableTypes.Int32;
        break;
      case Fmi2.fmi2ScalarVariableReal inputVar:
        VariableType = VariableTypes.Float64;
        if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
        {
          TypeDefinition = typeDefinitions[inputVar.declaredType];
        }

        break;
      case fmi2ScalarVariableBoolean:
        VariableType = VariableTypes.Boolean;
        break;
      case fmi2ScalarVariableString:
        VariableType = VariableTypes.String;
        break;
      case fmi2ScalarVariableEnumeration:
        VariableType = VariableTypes.EnumFmi2;
        break;
      default:
        throw new InvalidDataException($"The variable '{input.name}' has an unknown variable type.");
    }

    switch (input.causality)
    {
      case fmi2ScalarVariableCausality.parameter:
        Causality = Causalities.Parameter;
        break;
      case fmi2ScalarVariableCausality.calculatedParameter:
        Causality = Causalities.CalculatedParameter;
        break;
      case fmi2ScalarVariableCausality.input:
        Causality = Causalities.Input;
        break;
      case fmi2ScalarVariableCausality.output:
        Causality = Causalities.Output;
        break;
      case fmi2ScalarVariableCausality.local:
        Causality = Causalities.Local;
        break;
      case fmi2ScalarVariableCausality.independent:
        Causality = Causalities.Independent;
        break;
      default:
        throw new InvalidDataException($"The variable '{input.name}' has an unknown causality.");
    }

    if (true /*input.variabilitySpecified*/)
    {
      switch (input.variability)
      {
        case fmi2ScalarVariableVariability.constant:
          Variability = Variabilities.Constant;
          break;
        case fmi2ScalarVariableVariability.@fixed:
          Variability = Variabilities.Fixed;
          break;
        case fmi2ScalarVariableVariability.tunable:
          Variability = Variabilities.Tunable;
          break;
        case fmi2ScalarVariableVariability.discrete:
          Variability = Variabilities.Discrete;
          break;
        case fmi2ScalarVariableVariability.continuous:
          Variability = Variabilities.Continuous;
          break;
        default:
          throw new InvalidDataException($"The variable '{input.name}' has an unknown variability.");
      }
    }

    // NB: moved to ModelStructure in FMI3
    if (input.initialSpecified)
    {
      switch (input.initial)
      {
        case fmi2ScalarVariableInitial.exact:
          InitialValue = InitialValues.Exact;
          break;
        case fmi2ScalarVariableInitial.approx:
          InitialValue = InitialValues.Approx;
          break;
        case fmi2ScalarVariableInitial.calculated:
          InitialValue = InitialValues.Calculated;
          break;
        default:
          throw new InvalidDataException($"The variable '{input.name}' has an unknown initial value.");
      }
    }
    else
    {
      InitialValue = InitialValues.Unset;
      // calculate initial based on causality and variability
      switch (Causality)
      {
        case Causalities.Parameter:
        {
          if (Variability is Variabilities.Fixed or Variabilities.Tunable)
          {
            InitialValue = InitialValues.Exact;
          }

          break;
        }
        case Causalities.CalculatedParameter:
          if (Variability is Variabilities.Fixed or Variabilities.Tunable)
          {
            InitialValue = InitialValues.Calculated;
          }

          break;
        case Causalities.Input:
          // no sets allowed
          break;
        case Causalities.Output:
          if (Variability == Variabilities.Constant)
          {
            InitialValue = InitialValues.Exact;
          }
          else if (Variability is Variabilities.Discrete or Variabilities.Continuous)
          {
            InitialValue = InitialValues.Calculated;
          }

          break;
        case Causalities.Local:
          if (Variability == Variabilities.Constant)
          {
            InitialValue = InitialValues.Exact;
          }
          else if (Variability is Variabilities.Fixed or Variabilities.Tunable)
          {
            InitialValue = InitialValues.Calculated;
          }
          else if (Variability is Variabilities.Discrete or Variabilities.Continuous)
          {
            InitialValue = InitialValues.Calculated;
          }

          break;
        case Causalities.Independent:
          // no sets allowed
          break;
        case Causalities.StructuralParameter:
          // not available in FMI2
          break;
        default:
          break;
      }
    }

    var propInfo = input.Item.GetType().GetProperty("start");
    if (propInfo != null)
    {
      var res = propInfo.GetValue(input.Item);
      if (res != null)
      {
        Start = new[] { res };
      }
    }
  }

  private bool IsTypeDefInMap(string declaredType, Dictionary<string, TypeDefinition> typeDefinitions)
  {
    return typeDefinitions.ContainsKey(declaredType);
  }

  internal void InitializeArrayLength(ref Dictionary<uint /* ValueReference */, Variable> variables)
  {
    var prop = originalVariable.GetType().GetProperty("Dimension");
    if (prop != null)
    {
      var dimensions = prop.GetValue(originalVariable);
      if (dimensions == null)
      {
        // this variable is a scalar -> ArraySize = 1; skip array processing
        Dimensions = null;
        return;
      }

      if (dimensions is fmi3ArrayableVariableDimension[] dims)
      {
        Dimensions = GetDimensionSizeArray(dims, ref variables);
      }
      else
      {
        throw new DataConversionException("The dimension field did not contain the expected type.");
      }
    }
  }

  private ulong[] GetDimensionSizeArray(
    fmi3ArrayableVariableDimension[] originalDimensions,
    ref Dictionary<uint /* ValueReference */, Variable> variables)
  {
    var res = new ulong[originalDimensions.Length];
    for (var i = 0; i < originalDimensions.Length; i++)
    {
      if (originalDimensions[i].startSpecified)
      {
        res[i] = originalDimensions[i].start;
      }
      else if (originalDimensions[i].valueReferenceSpecified)
      {
        var v = variables[originalDimensions[i].valueReference];

        if (v.VariableType != VariableTypes.UInt64)
        {
          throw new ModelDescriptionException("The referenced dimension variable must be of type UInt64.");
        }

        if (v.Causality == Causalities.StructuralParameter || v.Variability == Variabilities.Constant)
        {
          if (v.Start != null)
          {
            res[i] = (ulong)v.Start[0];
          }
          else
          {
            throw new ModelDescriptionException("The referenced variable did not have a start value.");
          }
        }
        else
        {
          throw new ModelDescriptionException(
            "The referenced dimension variable must either be a structuralParameter or a constant.");
        }
      }
      else
      {
        throw new ModelDescriptionException("The dimension contained neither a start value nor a value reference.");
      }
    }

    return res;
  }

  public override string ToString()
  {
    var sb = new StringBuilder();
    sb.AppendLine($"{Name}");
    sb.AppendLine($"   Description: {Description}");
    sb.AppendLine($"   ValueReference: {ValueReference}");
    sb.AppendLine($"   Causality: {Causality.ToString()}");
    sb.AppendLine($"   Variability: {Variability.ToString()}");

    return sb.ToString();
  }
}
