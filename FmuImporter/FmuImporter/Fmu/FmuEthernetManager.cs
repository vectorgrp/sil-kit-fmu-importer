// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using System.Runtime.InteropServices;

namespace FmuImporter.Fmu;

public class FmuEthernetManager
{
  private IFmiBindingCommon Binding { get; }
  public List<Variable> OutputEthernetVariables { get; }
  public Dictionary<ulong /* valueRef */, Variable> InputEthernetVariables { get; }

  private readonly Action<LogSeverity, string> _logCallback;

  // default ctor if no Ethernet traffic to manage
  public FmuEthernetManager()
  {
    Binding = null!;
    OutputEthernetVariables = new List<Variable>();
    InputEthernetVariables = new Dictionary<ulong, Variable>();
    _logCallback = null!;
  }

  public FmuEthernetManager(
    IFmiBindingCommon binding,
    Action<LogSeverity, string> logCallback)
  {
    Binding = binding;
    _logCallback = logCallback;

    OutputEthernetVariables = new List<Variable>();
    InputEthernetVariables = new Dictionary<ulong, Variable>();
  }

  public void Initialize(ref Dictionary<uint /* ValueReference */, Variable> modelDescriptionVariables)
  {
    foreach (var (valueRef, modelDescriptionVariable) in modelDescriptionVariables)
    {
      if (modelDescriptionVariable.MimeType == null || 
          modelDescriptionVariable.MimeType.Contains(Terminal.Constants.EthernetMimeType) == false)
      {
        // other variables are handled in their own class
        continue;
      }

      // the variable is handled here
      modelDescriptionVariables.Remove(valueRef);

      // remove the corresponding ethernet clock
      var correspondingClockValueRef = modelDescriptionVariable.Clocks!.FirstOrDefault();
      modelDescriptionVariables.Remove(correspondingClockValueRef);

      switch (modelDescriptionVariable.Causality)
      {
        case Variable.Causalities.Output:
          OutputEthernetVariables.Add(modelDescriptionVariable);
          break;
        case Variable.Causalities.Input:
          InputEthernetVariables[modelDescriptionVariable.ValueReference] = modelDescriptionVariable;
          break;
      }
    }
  }

  public void SetEthernetData(Dictionary<uint /* valueRef */, List<byte[]>> receivedSilKitEthernetData)
  {
    foreach (var dataKvp in receivedSilKitEthernetData)
    {
      // set the corresponding clock. Assume that one Ethernet Rx_Data variable has only one associated Rx_Clock
      Binding.SetValue(InputEthernetVariables[dataKvp.Key].Clocks![0], new byte[] { 1 });
      // SetValue has to be called for every Ethernet frame
      foreach (var ethernetFrame in dataKvp.Value)
      {
        Binding.SetValue(dataKvp.Key, ethernetFrame, new int[] { ethernetFrame.Length });
      }
    }
  }

  public List<Tuple<uint, byte[]>> GetEthernetData()
  {
    var returnData = new List<Tuple<uint, byte[]>>();

    foreach (var ethernetVariable in OutputEthernetVariables)
    {
      // first get the value of the associated Tx_Clock
      // it is assumed that one Ethernet Tx_Data variable has only one associated Tx_Clock
      Binding.GetValue(new uint[] { ethernetVariable.Clocks![0] }, out var clockResult, VariableTypes.TriggeredClock);

      // if the associated clock has not been updated, don't get the Tx_Data
      if ((bool)clockResult.ResultArray[0].Values[0] == false)
      {
        continue;
      }

      // get fmi-ls-bus ethernet operations on Tx_Data
      Binding.GetValue(new uint[] { ethernetVariable.ValueReference }, out var result, VariableTypes.Binary);
      
      // The size of ResultArray is always one (the variable is a ethernet frame)
      var ethernetFrameData = result.ResultArray[0];

      for (var j = 0; j < ethernetFrameData.Values.Length; j++)
      {
        var binDataPtr = (IntPtr)ethernetFrameData.Values[j];
        var rawDataLength = (Int32)ethernetFrameData.ValueSizes[j];
        var binData = new byte[rawDataLength];
        Marshal.Copy(binDataPtr, binData, 0, rawDataLength);
        returnData.Add(Tuple.Create(ethernetFrameData.ValueReference, binData));
      }
    }
    return returnData;
  }
}
