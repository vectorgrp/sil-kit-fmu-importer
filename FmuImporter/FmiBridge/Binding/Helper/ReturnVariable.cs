// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.FmiModel.Internal;

namespace Fmi.Binding.Helper;

public class ReturnVariable
{
  public struct Variable
  {
    public uint ValueReference;
    public object[] Values;
    public IntPtr[] ValueSizes;
    public bool IsScalar;
    public VariableTypes Type;
  }

  public Variable[] ResultArray;

  private ReturnVariable()
  {
    ResultArray = Array.Empty<Variable>();
  }

  public static ReturnVariable CreateReturnVariable<T>(
    uint[] valueReferences,
    T[] values,
    nint nValues,
    ref ModelDescription modelDescription)
    where T : notnull
  {
    var result = new ReturnVariable();
    result.ResultArray = new Variable[valueReferences.Length];

    var indexCounter = 0;

    for (var i = 0; i < valueReferences.Length; i++)
    {
      var valueReference = valueReferences[i];
      var modelVar = modelDescription.Variables[valueReference];
      var arrayLength = modelVar.FlattenedArrayLength;

      var v = new Variable
      {
        ValueReference = valueReference,
        ValueSizes = Array.Empty<IntPtr>(),
        Values = new object[arrayLength],
        Type = modelVar.VariableType,
        IsScalar = modelVar.IsScalar
      };
      for (ulong j = 0; j < arrayLength; j++)
      {
        v.Values[j] = values[indexCounter];
        indexCounter++;
      }

      result.ResultArray[i] = v;
    }

    return result;
  }

  public static ReturnVariable CreateReturnVariable<T>(
    uint[] valueReferences,
    T[] values,
    nint nValues,
    ref ModelDescription modelDescription,
    size_t[] nValueSizes)
    where T : notnull
  {
    var result = new ReturnVariable();
    result.ResultArray = new Variable[valueReferences.Length];

    var indexCounter = 0;

    // outer loop -> value references
    for (var i = 0; i < valueReferences.Length; i++)
    {
      var valueReference = valueReferences[i];
      var modelVar = modelDescription.Variables[valueReference];
      var arrayLength = modelVar.FlattenedArrayLength;

      var v = new Variable
      {
        ValueReference = valueReference,
        ValueSizes = new IntPtr[arrayLength],
        // T ~ Array of Binaries -> IntPtr[]
        Values = new object[arrayLength],
        Type = modelVar.VariableType,
        IsScalar = modelVar.IsScalar
      };
      for (ulong j = 0; j < arrayLength; j++)
      {
        v.Values[j] = values[indexCounter];
        v.ValueSizes[j] = nValueSizes[indexCounter];
        indexCounter++;
      }

      result.ResultArray[i] = v;
    }

    return result;
  }
}
