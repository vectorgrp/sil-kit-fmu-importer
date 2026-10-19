// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using System.Runtime.InteropServices;

namespace FmuImporter.Fmu;

public class FmuLinManager
{
  private IFmiBindingCommon Binding { get; }
  public List<Variable> OutputLinVariables { get; }
  public Dictionary<ulong /* valueRef */, Variable> InputLinVariables { get; }

  private readonly Action<LogSeverity, string> _logCallback;

  // default ctor if no LIN traffic to manage
  public FmuLinManager()
  {
    Binding = null!;
    OutputLinVariables = new List<Variable>();
    InputLinVariables = new Dictionary<ulong, Variable>();
    _logCallback = null!;
  }

  public FmuLinManager(
    IFmiBindingCommon binding,
    Action<LogSeverity, string> logCallback)
  {
    Binding = binding;
    _logCallback = logCallback;
    OutputLinVariables = new List<Variable>();
    InputLinVariables = new Dictionary<ulong, Variable>();
  }

  public void Initialize(ref Dictionary<uint /* ValueReference */, Variable> modelDescriptionVariables,
    Dictionary<uint, bool> inputTxClocks)
  {
    foreach (var (valueRef, modelDescriptionVariable) in modelDescriptionVariables)
    {
      if (modelDescriptionVariable.MimeType == null ||
          modelDescriptionVariable.MimeType.Contains(Terminal.Constants.LinMimeType) == false)
      {
        continue;
      }

      if (modelDescriptionVariable.Causality == Variable.Causalities.Output &&
          Helpers.Helpers.IsInputTxClock(modelDescriptionVariable, modelDescriptionVariables))
      {
        inputTxClocks[modelDescriptionVariable.Clocks![0]] = false;
      }

      modelDescriptionVariables.Remove(valueRef);

      var correspondingClockValueRef = modelDescriptionVariable.Clocks!.FirstOrDefault();
      modelDescriptionVariables.Remove(correspondingClockValueRef);

      switch (modelDescriptionVariable.Causality)
      {
        case Variable.Causalities.Output:
          OutputLinVariables.Add(modelDescriptionVariable);
          break;
        case Variable.Causalities.Input:
          InputLinVariables[modelDescriptionVariable.ValueReference] = modelDescriptionVariable;
          break;
      }
    }
  }

  public void SetLinData(Dictionary<uint /* valueRef */, List<byte[]>> receivedSilKitLinData)
  {
    foreach (var dataKvp in receivedSilKitLinData)
    {
      // set the corresponding clock. Assume that one LIN Rx_Data variable has only one associated Rx_Clock
      Binding.SetValue(InputLinVariables[dataKvp.Key].Clocks![0], new byte[] { 1 });
      // SetValue has to be called for every received LIN operation
      foreach (var bytesForCertainLinOperation in dataKvp.Value)
      {
        Binding.SetValue(dataKvp.Key, bytesForCertainLinOperation, new int[] { bytesForCertainLinOperation.Length });
      }
    }
  }

  public List<Tuple<uint, byte[]>> GetLinData(Dictionary<uint, bool> inputTxClocks)
  {
    var returnData = new List<Tuple<uint, byte[]>>();

    foreach (var linVariable in OutputLinVariables)
    {
      // it is assumed that one LIN Tx_Data variable has only one associated Tx_Clock
      var clockRef = linVariable.Clocks![0];

      if (!Helpers.Helpers.IsActiveClock(Binding, inputTxClocks, clockRef))
      {
        continue;
      }

      Binding.GetValue(new uint[] { linVariable.ValueReference }, out var result, VariableTypes.Binary);

      var linBusData = result.ResultArray[0];

      for (var j = 0; j < linBusData.Values.Length; j++)
      {
        var binDataPtr = (IntPtr)linBusData.Values[j];
        var rawDataLength = (Int32)linBusData.ValueSizes[j];
        var binData = new byte[rawDataLength];
        Marshal.Copy(binDataPtr, binData, 0, rawDataLength);
        returnData.Add(Tuple.Create(linBusData.ValueReference, binData));
      }
    }

    return returnData;
  }
}
