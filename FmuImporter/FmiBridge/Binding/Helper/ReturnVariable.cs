using System.Reflection.Metadata.Ecma335;

namespace Fmi.Binding
{
  public struct ReturnVariable<T>
  {
    public T[] Values;
    public IntPtr NValues;

    public ReturnVariable(T[] values, nint nValues)
    {
      Values = values;
      NValues = nValues;
    }
  }
}
