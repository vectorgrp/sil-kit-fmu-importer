// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;

namespace Fmi.FmiModel.Internal;

public enum InternalTerminalKind
{
  UNKNOWN = 0,
  CAN = 1
}

public class Terminal
{
  public InternalTerminalKind InternalTerminalKind { get; set; }
  public string TerminalKind { get; set; }
  public string MatchingRule { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }

  public Dictionary<string /*Terminal Member variableName*/, TerminalMemberVariable> TerminalMemberVariables;

  public Dictionary<string /* terminal name */, Terminal>? NestedTerminals;


  public Terminal(fmi3Terminal input, ModelDescription modelDescription, Action<LogSeverity, string> logCallback)
  {
    TerminalMemberVariables = new Dictionary<string, TerminalMemberVariable>();
    NestedTerminals = new Dictionary<string, Terminal>();

    InternalTerminalKind = InternalTerminalKind.UNKNOWN;
    TerminalKind = input.terminalKind;
    MatchingRule = input.matchingRule;
    Name = input.name;
    Description = input.description;

    foreach (var terminalMember in input.TerminalMemberVariable)
    {
      var terminalMemberVariable = new TerminalMemberVariable
      {      
        VariableName = terminalMember.variableName,
        MemberName = terminalMember.memberName,
        VariableKind = terminalMember.variableKind        
      };

      if(modelDescription.NameToValueReference.TryGetValue(terminalMember.variableName, out var refValue))
      {
        terminalMemberVariable.CorrespondingValueReference = refValue;
      }
      else
      {
        terminalMemberVariable.CorrespondingValueReference = null;
        logCallback.Invoke(LogSeverity.Warning, $"No corresponding ModelVariable found in modelDescription for terminalMember with variableName {terminalMember.variableName}.");
      }
      

      if(!TerminalMemberVariables.TryAdd(terminalMember.variableName, terminalMemberVariable))
      {
        throw new TerminalsAndIconsException($"TerminalMemberVariable {terminalMember.variableName} already exists.");
      }
    }

    if (input.Terminal != null)
    {
      foreach (var nestedTerminal in input.Terminal)
      {       
        if(!NestedTerminals.TryAdd(nestedTerminal.name, new Terminal(nestedTerminal, modelDescription, logCallback)))
        {
          throw new TerminalsAndIconsException($"Terminal {nestedTerminal.name} already exists. Terminal names have to be unique.");
        }
      }
    }

    if (input.TerminalGraphicalRepresentation != null)
    {
      logCallback.Invoke(LogSeverity.Warning, "TerminalGraphicalRepresentation found in TerminalsAndIcons.xml. This is currently not supported by the SIL Kit FMU Importer.");
    }

    if (input.TerminalStreamMemberVariable != null)
    {
      logCallback.Invoke(LogSeverity.Warning, "TerminalStreamMemberVariable found in TerminalsAndIcons.xml. This is currently not supported by the SIL Kit FMU Importer.");
    }

    CheckTerminalKind(modelDescription);
  }

  private void CheckTerminalKind(ModelDescription modelDescription)
  {
    foreach(var pairNameMember in TerminalMemberVariables)
    {
      var vRef = pairNameMember.Value.CorrespondingValueReference;
      if (!vRef.HasValue)
      {
        break; // all terminal members have a corresponding value ref in fmi-ls-bus, so skip the next checks
      }

      if (modelDescription.Variables[vRef.Value].MimeType?.Contains("application/org.fmi-standard.fmi-ls-bus.can") == true)
      {
        // if a mimeType for fmi-ls-bus can has been found, validate the whole corresponding Terminal
        ValidateCANTerminal();
        break;
      }
    }
  }

  private void ValidateCANTerminal()
  {
    if ((MatchingRule != "org.fmi-ls-bus.transceiver") || (TerminalKind != "org.fmi-ls-bus.network-terminal"))
    {
      throw new TerminalsAndIconsException($"Terminal {Name} does not match fmi-ls-bus can " +
                                           $"matchingRule=\"org.fmi-ls-bus.transceiver\" or terminalKind=\"org.fmi-ls-bus.network-terminal\".");
    }

    InternalTerminalKind = InternalTerminalKind.CAN;
    foreach (var pairNameMember in TerminalMemberVariables)
    {
      if (pairNameMember.Value.MemberName is not ("Rx_Clock" or "Tx_Clock" or "Rx_Data" or "Tx_Data"))
      {
        throw new TerminalsAndIconsException($"TerminalMemberVariable {pairNameMember.Key} must match one of the " +
          $"following memberName: Rx_Clock, Tx_Clock, Rx_Data, Tx_Data.");
      }
      if (pairNameMember.Value.VariableKind != "signal")
      {
      throw new TerminalsAndIconsException($"TerminalMemberVariable {pairNameMember.Key} must have 'signal'" +
        $"as variableKind.");
      }
    }

    if ((NestedTerminals == null) || (NestedTerminals.Count == 0))
    {
      return;
    }

    if (NestedTerminals.Count > 1)
    {
      throw new TerminalsAndIconsException($"Terminal {Name} must contain only one nested terminal.");
    }

    var nestedTerminal = NestedTerminals.First().Value;
    if (nestedTerminal.Name != "Configuration")
    {
      throw new TerminalsAndIconsException($"Terminal {Name} contains the nested terminal " +
        $"{NestedTerminals.First().Key} with the name {nestedTerminal.Name}. It must be Configuration.");
    }
    if (nestedTerminal.TerminalKind != "org.fmi-ls-bus.network-terminal.configuration")
    {
      throw new TerminalsAndIconsException($"Terminal {Name} contains the nested terminal " +
        $"{NestedTerminals.First().Key} with the terminalKind {nestedTerminal.TerminalKind}. It must be " +
        $"org.fmi-ls-bus.network-terminal.configuration.");
    }
    if (nestedTerminal.MatchingRule != "bus")
    {
      throw new TerminalsAndIconsException($"Terminal {Name} contains the nested terminal " +
        $"{NestedTerminals.First().Key} with the matchingRule {nestedTerminal.MatchingRule}. It must be bus.");
    }
  }
}
