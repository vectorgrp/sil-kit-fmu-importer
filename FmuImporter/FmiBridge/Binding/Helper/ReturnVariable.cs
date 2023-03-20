using Fmi.FmiModel.Internal;

namespace Fmi.Binding;

public class ReturnVariable<T>
{
  public struct Variable
  {
    public uint ValueReference;
    public T[] Values;
    public IntPtr[] ValueSizes;
    public bool IsScalar;
  }

  public Variable[] ResultArray;

  private ReturnVariable()
  {
    ResultArray = Array.Empty<Variable>();
  }

  public static ReturnVariable<T> CreateReturnVariable(
    uint[] valueReferences,
    T[] values,
    nint nValues,
    ref ModelDescription modelDescription)
  {
    var result = new ReturnVariable<T>();
    result.ResultArray = new Variable[valueReferences.Length];

    int indexCounter = 0;

    for (int i = 0; i < valueReferences.Length; i++)
    {
      var valueReference = valueReferences[i];
      var modelVar = modelDescription.Variables[valueReference];
      var arrayLength = modelVar.FlattenedArrayLength;

      var v = new Variable
      {
        ValueReference = valueReference,
        ValueSizes = Array.Empty<IntPtr>(),
        Values = new T[arrayLength],
        IsScalar = (modelVar.Dimensions == null || modelVar.Dimensions.Length == 0)
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

  public static ReturnVariable<T> CreateReturnVariable(
    uint[] valueReferences,
    T[] values,
    nint nValues,
    ref ModelDescription modelDescription,
    size_t[] nValueSizes)
  {
    var result = new ReturnVariable<T>();
    result.ResultArray = new Variable[valueReferences.Length];

    int indexCounter = 0;

    // outer loop -> value references
    for (int i = 0; i < valueReferences.Length; i++)
    {
      var valueReference = valueReferences[i];
      var modelVar = modelDescription.Variables[valueReference];
      var arrayLength = modelVar.FlattenedArrayLength;

      var v = new Variable
      {
        ValueReference = valueReference,
        ValueSizes = new IntPtr[arrayLength],
        // T ~ Array of Binaries -> IntPtr[]
        Values = new T[arrayLength],
        IsScalar = (modelVar.Dimensions == null || modelVar.Dimensions.Length == 0)
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
