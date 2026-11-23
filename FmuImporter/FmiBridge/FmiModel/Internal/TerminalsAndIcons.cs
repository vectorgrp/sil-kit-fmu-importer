// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;
using Fmi3;

namespace Fmi.FmiModel.Internal;

public class TerminalsAndIcons
{
  public string FmiVersion { get; set; }
   
  public Dictionary<string /* terminal name */, Terminal> Terminals;

  public TerminalsAndIcons(fmiTerminalsAndIcons input, ModelDescription modelDescription, Action<LogSeverity, string> logCallback)
  {
    // init of local fields & properties
    Terminals = new Dictionary<string, Terminal>();

    FmiVersion = input.fmiVersion;

    foreach (var terminal in input.Terminals)
    {
      if (!Terminals.TryAdd(terminal.name, new Terminal(terminal, modelDescription, logCallback)))
      {
        throw new TerminalsAndIconsException($"Terminal {terminal.name} already exists. Terminal names have to be unique.");
      }
    }
        
    if (input.GraphicalRepresentation != null)
    {
      logCallback.Invoke(LogSeverity.Warning, "GraphicalRepresentation found in TerminalsAndIcons.xml. This is currently not supported by the SIL Kit FMU Importer.");
    }
  }

  /// <summary>
  ///   Returns the value references of all variables that belong to a recognized network (CAN, Ethernet, LIN) or
  ///   RPC terminal, including the variables of their nested terminals and the clocks of these variables.
  ///   Variables of terminals with an unknown kind are not included.
  /// </summary>
  public HashSet<uint> GetNetworkAndRpcTerminalValueReferences(ModelDescription modelDescription)
  {
    var valueRefs = new HashSet<uint>();
    foreach (var terminal in Terminals.Values)
    {
      if (terminal.InternalTerminalKind != InternalTerminalKind.UNKNOWN)
      {
        AddTerminalValueReferences(terminal, modelDescription, valueRefs);
      }
    }

    return valueRefs;
  }

  private static void AddTerminalValueReferences(Terminal terminal, ModelDescription modelDescription, HashSet<uint> valueRefs)
  {
    foreach (var memberVariable in terminal.TerminalMemberVariables.Values)
    {
      if (!memberVariable.CorrespondingValueReference.HasValue)
      {
        continue;
      }

      var valueRef = memberVariable.CorrespondingValueReference.Value;
      valueRefs.Add(valueRef);
      if (modelDescription.Variables.TryGetValue(valueRef, out var variable) && variable.Clocks != null)
      {
        valueRefs.UnionWith(variable.Clocks);
      }
    }

    if (terminal.NestedTerminals == null)
    {
      return;
    }

    foreach (var nestedTerminal in terminal.NestedTerminals.Values)
    {
      AddTerminalValueReferences(nestedTerminal, modelDescription, valueRefs);
    }
  }
}

