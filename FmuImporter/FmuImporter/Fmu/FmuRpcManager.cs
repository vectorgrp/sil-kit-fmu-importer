// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;
using Fmi.Binding;
using Fmi.FmiModel.Internal;
using System.Runtime.InteropServices;

namespace FmuImporter.Fmu;

public class FmuRpcManager
{
  private IFmiBindingCommon Binding { get; }
  public List<Variable> OutputIdVariables { get; }
  public Dictionary<uint /* vRef Rx_Id */, Variable> InputIdVariables { get; }
  public Dictionary<uint /* vRef Rx_id */, uint? /* vRef Rx_Args */> VrefRxIdArgs { get; }
  public Dictionary<uint /* vRef Tx_Id */, uint? /* vRef Tx_Args */> VrefTxIdArgs { get; }

  // default ctor if no CAN traffic to manage
  public FmuRpcManager()
  { 
    Binding = null!;
    OutputIdVariables = new List<Variable>();
    InputIdVariables = new Dictionary<uint, Variable>();
    VrefRxIdArgs = new Dictionary<uint, uint?>();
    VrefTxIdArgs = new Dictionary<uint, uint?>();
  }

  public FmuRpcManager(IFmiBindingCommon binding, Action<LogSeverity, string> logCallback) : this()
  {
    Binding = binding;
  }

  public void Initialize(ref Dictionary<uint /* vRef */, Variable> modelDescriptionVariables)
  {
    var flatRx = VrefRxIdArgs.SelectMany(kvp => kvp.Value.HasValue ? new[] { kvp.Key, kvp.Value.Value } : new[] { kvp.Key }).ToHashSet();
    var flatTx = VrefTxIdArgs.SelectMany(kvp => kvp.Value.HasValue ? new[] { kvp.Key, kvp.Value.Value } : new[] { kvp.Key }).ToHashSet();

    var valueRefsToRemove = new List<uint>();

    foreach (var (valueRef, modelDescriptionVariable) in modelDescriptionVariables)
    {
      if (flatRx.Contains(valueRef) || flatTx.Contains(valueRef))
      {
        // Handle the variable here and remove it from the main list afterwards
        valueRefsToRemove.Add(valueRef);

        // remove the corresponding clock
        var correspondingClockValueRef = modelDescriptionVariable.Clocks!.FirstOrDefault();
        valueRefsToRemove.Add(correspondingClockValueRef);

        if (modelDescriptionVariable.Name.Contains("Id"))
        {
          // Handle Id variables here
          switch (modelDescriptionVariable.Causality)
          {
            case Variable.Causalities.Output:
              OutputIdVariables.Add(modelDescriptionVariable);
              break;
            case Variable.Causalities.Input:
              InputIdVariables[valueRef] = modelDescriptionVariable;
              break;
          }
        }
      }
    }

    foreach (var key in valueRefsToRemove)
    {
      modelDescriptionVariables.Remove(key);
    }
  }

  public void SetData(Dictionary<uint /* vRef Rx_Id */, List<Tuple<ulong /* Rx_Id */, byte[]?>>> receivedSilKitRpcResultEvent)
  {
    foreach (var dataKvp in receivedSilKitRpcResultEvent)
    {
      // set the Rx clock
      Binding.SetValue(InputIdVariables[dataKvp.Key].Clocks![0], new byte[] { 1 });

      // SetValue has to be called for every Rx_Id and Rx_Args
      foreach (var idData in dataKvp.Value)
      {
        Binding.SetValue(dataKvp.Key, BitConverter.GetBytes(idData.Item1));

        if (idData.Item2 is not null)
        {
          // try to get the vRef Rx_Args and set the bytes
          VrefRxIdArgs.TryGetValue(dataKvp.Key, out var vRefRx_Args);
          if (vRefRx_Args.HasValue)
          {
            Binding.SetValue(vRefRx_Args.Value, idData.Item2, new int[] { idData.Item2.Length });
          }
        }
      }
    }
  }

  public List<Tuple<uint /* vRef Tx_Id */, Tuple<ulong /* Tx_Id */, byte[]?>>> GetOperations()
  {
    var returnData = new List<Tuple<uint, Tuple<ulong, byte[]?>>>();

    foreach (var idVar in OutputIdVariables)
    {
      // first get the value of the associated Tx clock
      // it is assumed that one RPC Tx_Id variable has only one associated Tx clock
      Binding.GetValue(new uint[] { idVar.Clocks![0] }, out var clockResult, VariableTypes.TriggeredClock);

      // if the associated clock has not been updated, don't get the Tx_Id & Tx_Args
      if ((bool)clockResult.ResultArray[0].Values[0] == false)
      {
        continue;
      }

      Binding.GetValue(new uint[] { idVar.ValueReference }, out var resultId, VariableTypes.UInt64);
      VrefTxIdArgs.TryGetValue(idVar.ValueReference, out var vRefTxArgs);

      var id = (ulong)resultId.ResultArray[0].Values[0];

      if (vRefTxArgs.HasValue)
      {
        Binding.GetValue(new uint[] { vRefTxArgs.Value }, out var resultArgs, VariableTypes.Binary);
        
        var args = resultArgs.ResultArray[0];
        for (var j = 0; j < args.Values.Length; j++)
        {
          var binDataPtr = (IntPtr)args.Values[j];
          var binDataLength = (Int32)args.ValueSizes[j];
          var binData = new byte[binDataLength];
          Marshal.Copy(binDataPtr, binData, 0, binDataLength);

          Tuple<ulong, byte[]?> mtuple = Tuple.Create(id, (byte[]?)binData);
          returnData.Add(new Tuple<uint, Tuple<ulong, byte[]?>>(resultId.ResultArray[0].ValueReference, mtuple));
        }
      }
      else
      {
        // no args
        Tuple<ulong, byte[]?> mtuple = Tuple.Create(id, (byte[]?)null);
        returnData.Add(new Tuple<uint, Tuple<ulong, byte[]?>>(resultId.ResultArray[0].ValueReference, mtuple));
      }
    }
    return returnData;
  }

  public void AddTxValueRefs((uint vRef_TxId, uint? vRef_TxArgs) vRefs)
  {
    VrefTxIdArgs[vRefs.vRef_TxId] = vRefs.vRef_TxArgs;
  }

  public void AddRxValueRefs((uint vRef_RxId, uint? vRef_RxArgs) vRefs)
  {
    VrefRxIdArgs[vRefs.vRef_RxId] = vRefs.vRef_RxArgs;
  }
}
