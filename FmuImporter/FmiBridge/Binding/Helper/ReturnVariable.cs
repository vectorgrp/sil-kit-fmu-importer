using System.Reflection.Metadata.Ecma335;

namespace Fmi.Binding
{
  public struct ReturnVariable<T>
  {
    public T[] Values;
    public IntPtr NValues;
    public IntPtr[]? NValueSizes;

    public ReturnVariable(T[] values, nint nValues)
    {
      Values = values;
      NValues = nValues;
      NValueSizes = null;
    }

    public ReturnVariable(T[] values, nint nValues, IntPtr[] nValueSizes) : this(values, nValues)
    {
      NValueSizes = nValueSizes;
    }
  }
}
