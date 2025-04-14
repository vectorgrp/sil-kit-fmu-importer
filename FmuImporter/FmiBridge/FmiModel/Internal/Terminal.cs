// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;

namespace Fmi.FmiModel.Internal;

public class Terminal
{
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
  }
}
