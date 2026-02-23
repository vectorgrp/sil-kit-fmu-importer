// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.Text;
using Fmi.Exceptions;
using Fmi2;
using Fmi3;

namespace Fmi.FmiModel.Internal;

public class Variable
{
  private readonly Fmi3.fmi3AbstractVariable? _originalVariable;

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
  public uint[]? Clocks { get; set; }
  public InitialValues InitialValue { get; set; }

  public VariableTypes VariableType { get; }

  public string? MimeType { get; }
  
  // used for (De-)Serialization of Binaries with MimeType "*.vcdl.struct.*"
  // e.g. present in FMUs exported by Vector vVIRTUALtarget
  public int? VcdlStructMaxSize { get; }

  public object[]? Start { get; set; }

  public ulong[]? Dimensions { get; set; }

  public TypeDefinition? TypeDefinition { get; set; }

  public ulong FlattenedArrayLength { get; private set; } = 1;
  public bool IsScalar { get; set; } = true;

  public Variable(
    fmi3AbstractVariable input, Dictionary<string, TypeDefinition> typeDefinitions,
    Action<LogSeverity, string> logCallback)
  {
    _originalVariable = input;

    Name = input.name;
    ValueReference = input.valueReference;
    Description = input.description;

    Clocks = (input.clocks.Count > 0)
    ? input.clocks.ToArray()
    : null;

    switch (input)
    {
      case fmi3Float32 inputVar:
        VariableType = VariableTypes.Float32;
        if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
        {
          TypeDefinition = typeDefinitions[inputVar.declaredType];
        }

        break;
      case fmi3Float64 inputVar:
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
        var downcast = (Fmi3.fmi3Binary)input;
        MimeType = downcast.mimeType;
        break;
      case fmi3Enumeration inputVar:
        VariableType = VariableTypes.EnumFmi3;
        if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
        {
          TypeDefinition = typeDefinitions[inputVar.declaredType];
        }
        else
        {
          throw new ModelDescriptionException(
            $"The enumerator of variable '{Name}' is unknown.");
        }

        break;

      case fmi3Clock clock:
        if (clock.intervalVariability.Equals(Ifmi3ClockAttributesintervalVariability.triggered))
        {
          VariableType = VariableTypes.TriggeredClock;
        }
        else
        {
          logCallback.Invoke(LogSeverity.Warning, $"Type 'Clock' (with IntervalVariability != triggered) of variable '{Name}' is not supported yet. Discarding variable.");
          VariableType = VariableTypes.TimeBasedClock;
        }
        break;

      default:
        throw new InvalidDataException($"The FMI 3 datatype of variable {Name} is unknown.");
    }

    switch (input.causality)
    {
      case fmi3AbstractVariablecausality.parameter:
        Causality = Causalities.Parameter;
        break;
      case fmi3AbstractVariablecausality.calculatedParameter:
        Causality = Causalities.CalculatedParameter;
        break;
      case fmi3AbstractVariablecausality.input:
        Causality = Causalities.Input;
        break;
      case fmi3AbstractVariablecausality.output:
        Causality = Causalities.Output;
        break;
      case fmi3AbstractVariablecausality.local:
        Causality = Causalities.Local;
        break;
      case fmi3AbstractVariablecausality.independent:
        Causality = Causalities.Independent;
        break;
      case fmi3AbstractVariablecausality.structuralParameter:
        Causality = Causalities.StructuralParameter;
        break;
      default:
        throw new InvalidDataException($"The variable '{input.name}' has an unknown causality.");
    }

    if (input.variabilitySpecified)
    {
      switch (input.variability)
      {
        case fmi3AbstractVariablevariability.constant:
          Variability = Variabilities.Constant;
          break;
        case fmi3AbstractVariablevariability.Item_fixed:
          Variability = Variabilities.Fixed;
          break;
        case fmi3AbstractVariablevariability.tunable:
          Variability = Variabilities.Tunable;
          break;
        case fmi3AbstractVariablevariability.discrete:
          Variability = Variabilities.Discrete;
          break;
        case fmi3AbstractVariablevariability.continuous:
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
        switch (res)
        {
          case Array array:
            {
              var arrLength = array.Length;
              Start = new object[arrLength];
              for (var i = 0; i < arrLength; i++)
              {
                Start[i] = array.GetValue(i) ?? throw new InvalidOperationException(
                  $"Error while getting the start field for {input.name} with the following reference: {input.valueReference}");
              }
              break;
            }

          case System.Collections.IEnumerable enumerable when res.GetType().IsGenericType:
            {
              // Handles Collection<T>, List<T>, etc. into a flat object[]
              var tmp = new List<object>();
              foreach (var item in enumerable)
              {
                if (item != null)
                {
                  tmp.Add(item);
                }
              }

              Start = tmp.Count > 0 ? tmp.ToArray() : null;
              break;
            }

          case List<byte> byteList:
            {
              // special case
              Start = byteList.Select(v => (object)v).ToArray();
              break;
            }

          default:
            {
              Start = new[] { res };
              break;
            }
        }
      }
    }

    // Determine scalar vs. array based on the presence of Dimension information
    if (_originalVariable is Fmi3.Ifmi3Dimensions dimSource &&
        dimSource.Dimension != null &&
        dimSource.Dimension.Count > 0)
    {
      IsScalar = false;
    }
    else
    {
      IsScalar = true;
    }
    if (VariableType is VariableTypes.Binary &&
        MimeType is not null &&
        input.GetType().GetProperty("maxSize")?.GetValue(input) is not null and var maxSize &&
        MimeType.Contains(".vcdl.struct."))
    {
      VcdlStructMaxSize = Convert.ToInt32(maxSize.ToString());
    }
  }

  public Variable(fmi2ScalarVariable input, Dictionary<string, TypeDefinition> typeDefinitions)
  {
    Name = input.name;
    ValueReference = input.valueReference;
    Description = input.description;

    // Determine variable type based on concrete value property in regenerated FMI2 model
    if (input.Integer != null)
    {
      VariableType = VariableTypes.Int32;
    }
    else if (input.Real != null)
    {
      var inputVar = input.Real;
      VariableType = VariableTypes.Float64;
      if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
      {
        TypeDefinition = typeDefinitions[inputVar.declaredType];
      }
    }
    else if (input.Boolean != null)
    {
      VariableType = VariableTypes.Boolean;
    }
    else if (input.String != null)
    {
      VariableType = VariableTypes.String;
    }
    else if (input.Enumeration != null)
    {
      var inputVar = input.Enumeration;
      VariableType = VariableTypes.EnumFmi2;
      if (!string.IsNullOrEmpty(inputVar.declaredType) && IsTypeDefInMap(inputVar.declaredType, typeDefinitions))
      {
        TypeDefinition = typeDefinitions[inputVar.declaredType];
      }
      else
      {
        throw new ModelDescriptionException(
          $"The enumerator of variable '{Name}' is unknown.");
      }
    }
    else
    {
      throw new InvalidDataException($"The variable '{input.name}' has an unknown variable type.");
    }

    switch (input.causality)
    {
      case fmi2ScalarVariablecausality.parameter:
        Causality = Causalities.Parameter;
        break;
      case fmi2ScalarVariablecausality.calculatedParameter:
        Causality = Causalities.CalculatedParameter;
        break;
      case fmi2ScalarVariablecausality.input:
        Causality = Causalities.Input;
        break;
      case fmi2ScalarVariablecausality.output:
        Causality = Causalities.Output;
        break;
      case fmi2ScalarVariablecausality.local:
        Causality = Causalities.Local;
        break;
      case fmi2ScalarVariablecausality.independent:
        Causality = Causalities.Independent;
        break;
      default:
        throw new InvalidDataException($"The variable '{input.name}' has an unknown causality.");
    }

    if (true /*input.variabilitySpecified*/)
    {
      switch (input.variability)
      {
        case fmi2ScalarVariablevariability.constant:
          Variability = Variabilities.Constant;
          break;
        case fmi2ScalarVariablevariability.@Item_fixed:
          Variability = Variabilities.Fixed;
          break;
        case fmi2ScalarVariablevariability.tunable:
          Variability = Variabilities.Tunable;
          break;
        case fmi2ScalarVariablevariability.discrete:
          Variability = Variabilities.Discrete;
          break;
        case fmi2ScalarVariablevariability.continuous:
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
        case fmi2ScalarVariableinitial.exact:
          InitialValue = InitialValues.Exact;
          break;
        case fmi2ScalarVariableinitial.approx:
          InitialValue = InitialValues.Approx;
          break;
        case fmi2ScalarVariableinitial.calculated:
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

    // read "start" property from concrete value container
    object? valueContainer = input.Integer ?? input.Real ?? input.Boolean ?? input.String ?? (object?)input.Enumeration;

    if (valueContainer != null)
    {
      var propInfo2 = valueContainer.GetType().GetProperty("start");
      if (propInfo2 != null)
      {
        var res = propInfo2.GetValue(valueContainer);
        if (res != null)
        {
          Start = new[] { res };
        }
      }
    }
  }

  private bool IsTypeDefInMap(string declaredType, Dictionary<string, TypeDefinition> typeDefinitions)
  {
    return typeDefinitions.ContainsKey(declaredType);
  }

  public void InitializeArrayLength(Dictionary<uint /* ValueReference */, Variable> variables)
  {
    if (_originalVariable == null)
    {
      throw new NullReferenceException(
        $"Tried to access {nameof(_originalVariable)}, which was not initialized correctly.");
    }

    var prop = _originalVariable.GetType().GetProperty("Dimension");
    if (prop == null)
    {
      Dimensions = null;
      IsScalar = true;
      FlattenedArrayLength = 1;
      return;
    }

    var dimensions = prop.GetValue(_originalVariable);
    if (dimensions == null)
    {
      Dimensions = null;
      IsScalar = true;
      FlattenedArrayLength = 1;
      return;
    }

    // dimensions is expected to be a Collection<fmi3DimensionsDimension>
    if (dimensions is System.Collections.ObjectModel.Collection<Fmi3.fmi3DimensionsDimension> dimCollection)
    {
      if (dimCollection.Count == 0)
      {
        // No dimensions defined -> treat as scalar
        Dimensions = null;
        IsScalar = true;
        FlattenedArrayLength = 1;
        return;
      }

      var dimsArray = dimCollection.ToArray();
      Dimensions = GetDimensionSizeArray(dimsArray, variables);

      FlattenedArrayLength = 1;
      foreach (var dim in Dimensions)
      {
        FlattenedArrayLength *= dim;
      }

      IsScalar = false;
    }
    else
    {
      throw new DataConversionException(
        $"The dimension field did not contain the expected type. Exception thrown by " +
        $"{_originalVariable.name} with the following value reference: {_originalVariable.valueReference}");
    }
  }

  private ulong[] GetDimensionSizeArray(
    Fmi3.fmi3DimensionsDimension[] originalDimensions,
    Dictionary<uint /* ValueReference */, Variable> variables)
  {
    var res = new ulong[originalDimensions.Length];
    for (var i = 0; i < originalDimensions.Length; i++)
    {
      var dim = originalDimensions[i];

      if (dim.startSpecified)
      {
        res[i] = dim.start;
      }
      else if (dim.valueReferenceSpecified)
      {
        var v = variables[dim.valueReference];

        if (v.VariableType != VariableTypes.UInt64)
        {
          throw new ModelDescriptionException(
            $"The referenced dimension variable must be of type UInt64. Exception " +
            $"thrown by {v.Name} with the following value reference: {v.ValueReference}");
        }

        if (v.Causality == Causalities.StructuralParameter || v.Variability == Variabilities.Constant)
        {
          if (v.Start != null && v.Start.Length > 0)
          {
            var raw = v.Start[0];

            // If raw is a collection (e.g. Collection<ulong>), take its first element
            if (raw is System.Collections.IEnumerable enumerable && raw is not string)
            {
              var enumerator = enumerable.GetEnumerator();
              if (enumerator.MoveNext())
              {
                raw = enumerator.Current;
              }
            }

            if (raw is string s)
            {
              res[i] = ulong.Parse(s);
            }
            else if (raw is ulong u)
            {
              res[i] = u;
            }
            else if (raw is System.IConvertible)
            {
              res[i] = System.Convert.ToUInt64(raw);
            }
            else
            {
              throw new ModelDescriptionException(
                $"The referenced dimension variable start value is not convertible to UInt64. Exception thrown " +
                $"by {v.Name} with the following value reference: {v.ValueReference}");
            }
          }
          else
          {
            throw new ModelDescriptionException(
              $"The referenced variable did not have a start value. Exception thrown " +
              $"by {v.Name} with the following value reference: {v.ValueReference}");
          }
        }
        else
        {
          throw new ModelDescriptionException(
            $"The referenced dimension variable must either be a structuralParameter or a constant. Exception thrown " +
            $"by {v.Name} with the following value reference: {v.ValueReference}");
        }
      }
      else
      {
        throw new ModelDescriptionException(
          "The dimension contained neither a start value nor a value reference. " +
          $"Exception thrown by the following value reference: {variables[dim.valueReference]}");
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
