// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using FmuImporter.Config;
using System.Runtime.InteropServices;

namespace FmuImporter.Fmu;

public class FmuCanManager
{
  private IFmiBindingCommon Binding { get; }
  public List<Variable> OutputCanVariables { get; }
  public Dictionary<ulong /* valueRef */, Variable> InputCanVariables { get; }

  private readonly Action<LogSeverity, string> _logCallback;

  // default ctor if no CAN traffic to manage
  public FmuCanManager()
  {
    Binding = null!;
    OutputCanVariables = new List<Variable>();
    InputCanVariables = new Dictionary<ulong, Variable>();
    _logCallback = null!;
  }

  public FmuCanManager(
    IFmiBindingCommon binding,
    Action<LogSeverity, string> logCallback)
  {
    Binding = binding;
    _logCallback = logCallback;

    OutputCanVariables = new List<Variable>();
    InputCanVariables = new Dictionary<ulong, Variable>();
  }

  public void Initialize(ref Dictionary<uint /* ValueReference */, Variable> modelDescriptionVariables)
  {
    foreach (var (valueRef, modelDescriptionVariable) in modelDescriptionVariables)
    {
      if (modelDescriptionVariable.MimeType == null || 
          modelDescriptionVariable.MimeType.Contains("application/org.fmi-standard.fmi-ls-bus.can") == false)
      {
        // other variables are handled in their own class
        continue;
      }

      // than the variable is handled here
      modelDescriptionVariables.Remove(valueRef);

      // remove the corresponding can clock
      var correspondingClockValueRef = modelDescriptionVariable.Clocks!.FirstOrDefault();
      modelDescriptionVariables.Remove(correspondingClockValueRef);

      switch (modelDescriptionVariable.Causality)
      {
        case Variable.Causalities.Output:
          OutputCanVariables.Add(modelDescriptionVariable);
          break;
        case Variable.Causalities.Input:
          InputCanVariables[modelDescriptionVariable.ValueReference] = modelDescriptionVariable;
          break;
      }
    }
  }

  public void SetCanData(Dictionary<uint /* valueRef */, List<byte[]>> receivedSilKitCanData)
  {
    foreach (var dataKvp in receivedSilKitCanData)
    {
      // set the corresponding clock. Assume that one CAN Rx_Data variable has only one associated Rx_Clock
      Binding.SetValue(InputCanVariables[dataKvp.Key].Clocks![0], new byte[] { 1 });
      // SetValue has to be called for every CAN id
      foreach (var bytesForCertainCanId in dataKvp.Value)
      {
        Binding.SetValue(dataKvp.Key, bytesForCertainCanId, new int[] { bytesForCertainCanId.Length });
      }
    }
  }

  public List<Tuple<uint, byte[]>> GetCanData()
  {
    var returnData = new List<Tuple<uint, byte[]>>();

    foreach (var canVariable in OutputCanVariables)
    {
      // first get the value of the associated Tx_Clock
      // it is assumed that one CAN Tx_Data variable has only one associated Tx_Clock
      Binding.GetValue(new uint[] { canVariable.Clocks![0] }, out var clockResult, VariableTypes.TriggeredClock);

      // if the associated clock has not been updated, don't get the Tx_Data
      if ((bool)clockResult.ResultArray[0].Values[0] == false)
      {
        continue;
      }

      // get fmi-ls-bus can operations on Tx_Data
      Binding.GetValue(new uint[] { canVariable.ValueReference }, out var result, VariableTypes.Binary);
      
      // The size of ResultArray is always one (the variable is a bus)
      var canBusData = result.ResultArray[0];

      for (var j = 0; j < canBusData.Values.Length; j++)
      {
        var binDataPtr = (IntPtr)canBusData.Values[j];
        var rawDataLength = (Int32)canBusData.ValueSizes[j];
        var binData = new byte[rawDataLength];
        Marshal.Copy(binDataPtr, binData, 0, rawDataLength);
        returnData.Add(Tuple.Create(canBusData.ValueReference, binData));
      }
    }
    return returnData;
  }
}
