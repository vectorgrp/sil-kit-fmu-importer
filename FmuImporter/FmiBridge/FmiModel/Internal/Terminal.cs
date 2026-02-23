// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi.Exceptions;

namespace Fmi.FmiModel.Internal;

public enum InternalTerminalKind
{
  UNKNOWN = 0,
  CAN = 1,
  RPC_CLIENT = 2,
  RPC_SERVER = 3
}

public class Terminal
{
  public static class Constants
  {
    // CAN Terminal Constants
    public const string CanMimeType = "application/org.fmi-standard.fmi-ls-bus.can";
    public const string CanTerminalKind = "org.fmi-ls-bus.network-terminal";
    public const string CanMatchingRule = "org.fmi-ls-bus.transceiver";
    public const string CanNestedTerminalKind = "org.fmi-ls-bus.network-terminal.configuration";
    public const string CanNestedMatchingRule = "bus";
    public const string CanNestedTerminalName = "Configuration";

    // RPC Terminal Constants
    public const string RpcTerminalKind = "vnd.vector.operation-terminal.v1";
    public const string RpcMatchingRule = "plug";

    // Member Names
    public const string SignalVariableKind = "signal";
  }

  public InternalTerminalKind InternalTerminalKind { get; set; }
  public string TerminalKind { get; set; }
  public string MatchingRule { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }

  public Dictionary<string /*Terminal Member variableName*/, TerminalMemberVariable> TerminalMemberVariables;

  public Dictionary<string /* terminal name */, Terminal>? NestedTerminals;


  public Terminal(Fmi3.fmi3Terminal input, ModelDescription modelDescription, Action<LogSeverity, string> logCallback)
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

    if (input.Terminal.Count > 0)
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

    if (input.TerminalStreamMemberVariable.Count > 0)
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
        break; // all terminal members have a corresponding value ref in fmi-ls-bus and rpc, so skip the next checks
      }

      var variable = modelDescription.Variables[vRef.Value];

      if (variable.MimeType?.Contains(Constants.CanMimeType) == true)
      {
        // if a mimeType for fmi-ls-bus can has been found, validate the whole corresponding Terminal
        ValidateCANTerminal();
        break;
      }
      else if (TerminalKind.Equals(Constants.RpcTerminalKind))
      {
        if (variable.Name.Contains("Tx_CallId") || variable.Name.Contains("Rx_ReturnId"))
        {
          InternalTerminalKind = InternalTerminalKind.RPC_CLIENT;
          ValidateRPCTerminal();
          break;
        }
        else if (variable.Name.Contains("Rx_CallId") || variable.Name.Contains("Tx_ReturnId"))
        {
          InternalTerminalKind = InternalTerminalKind.RPC_SERVER;
          ValidateRPCTerminal();
          break;
        }
      }
    }
  }

  private void ValidateCANTerminal()
  {
    if ((MatchingRule != Constants.CanMatchingRule) || (TerminalKind != Constants.CanTerminalKind))
    {
      throw new TerminalsAndIconsException($"Terminal {Name} does not match fmi-ls-bus can " +
                                           $"matchingRule=\"{Constants.CanMatchingRule}\" or terminalKind=\"{Constants.CanTerminalKind}\".");
    }

    InternalTerminalKind = InternalTerminalKind.CAN;
    foreach (var pairNameMember in TerminalMemberVariables)
    {
      if (pairNameMember.Value.MemberName is not ("Rx_Clock" or "Tx_Clock" or "Rx_Data" or "Tx_Data"))
      {
        throw new TerminalsAndIconsException($"TerminalMemberVariable {pairNameMember.Key} must match one of the " +
          $"following memberName: Rx_Clock, Tx_Clock, Rx_Data, Tx_Data.");
      }
      if (pairNameMember.Value.VariableKind != Constants.SignalVariableKind)
      {
      throw new TerminalsAndIconsException($"TerminalMemberVariable {pairNameMember.Key} must have '{Constants.SignalVariableKind}'" +
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
    if (nestedTerminal.Name != Constants.CanNestedTerminalName)
    {
      throw new TerminalsAndIconsException($"Terminal {Name} contains the nested terminal " +
        $"{NestedTerminals.First().Key} with the name {nestedTerminal.Name}. It must be {Constants.CanNestedTerminalName}.");
    }
    if (nestedTerminal.TerminalKind != Constants.CanNestedTerminalKind)
    {
      throw new TerminalsAndIconsException($"Terminal {Name} contains the nested terminal " +
        $"{NestedTerminals.First().Key} with the terminalKind {nestedTerminal.TerminalKind}. It must be " +
        $"{Constants.CanNestedTerminalKind}.");
    }
    if (nestedTerminal.MatchingRule != Constants.CanNestedMatchingRule)
    {
      throw new TerminalsAndIconsException($"Terminal {Name} contains the nested terminal " +
        $"{NestedTerminals.First().Key} with the matchingRule {nestedTerminal.MatchingRule}. It must be {Constants.CanNestedMatchingRule}.");
    }
  }

  public void ValidateRPCTerminal()
  {
    if (MatchingRule != Constants.RpcMatchingRule || TerminalKind != Constants.RpcTerminalKind)
    {
      throw new TerminalsAndIconsException($"Terminal {Name} does not match RPC terminal " +
                                           $"matchingRule=\"{Constants.RpcMatchingRule}\" or terminalKind=\"{Constants.RpcTerminalKind}\".");
    }

    // Define required and optional variables based on terminal type
    var (requiredVariables, optionalVariables) = InternalTerminalKind switch
    {
      InternalTerminalKind.RPC_CLIENT => (
        Required: new[] { "Tx_CallId", "Tx_Call", "Rx_ReturnId", "Rx_Return" },
        Optional: new[] { "Tx_CallArgs", "Rx_ReturnArgs" }
      ),
      InternalTerminalKind.RPC_SERVER => (
        Required: new[] { "Rx_CallId", "Rx_Call", "Tx_ReturnId", "Tx_Return" },
        Optional: new[] { "Rx_CallArgs", "Tx_ReturnArgs" }
      ),
      _ => throw new TerminalsAndIconsException($"Terminal {Name} has invalid InternalTerminalKind for RPC validation: {InternalTerminalKind}")
    };

    // local function to validate RPC variables
    void validateRpcVariables(string[] required, string[] optional)
    {
      var allAllowedVariables = required.Concat(optional).ToHashSet();
      var foundVariables = new HashSet<string>();

      // validate each terminal member variable
      foreach (var pairNameMember in TerminalMemberVariables)
      {
        var variableName = pairNameMember.Key;
        var memberVariable = pairNameMember.Value;

        // check variable kind
        if (memberVariable.VariableKind != Constants.SignalVariableKind)
        {
          throw new TerminalsAndIconsException($"TerminalMemberVariable {variableName} must have '{Constants.SignalVariableKind}' " +
                                               $"as variableKind.");
        }

        var memberName = memberVariable.MemberName;
        if (memberName != null)
        {
          foundVariables.Add(memberName);

          // check if memberName is allowed
          if (!allAllowedVariables.Contains(memberName))
          {
            throw new TerminalsAndIconsException($"Terminal {Name} contains unexpected RPC terminal member variable " +
                                                 $"with memberName '{memberName}'. Expected variables: {string.Join(", ", allAllowedVariables)}.");
          }

          // check if variableName contains memberName
          if (!variableName.Contains(memberName))
          {
            throw new TerminalsAndIconsException($"TerminalMemberVariable {variableName} must contain its memberName '{memberName}' " +
                                                 $"in the variable name.");
          }
        }
      }

      // check that all required variables are present
      foreach (var requiredVar in required)
      {
        if (!foundVariables.Contains(requiredVar))
        {
          throw new TerminalsAndIconsException($"Terminal {Name} is missing required RPC terminal member variable " +
                                               $"with memberName '{requiredVar}'.");
        }
      }
    }

    // validate with the specific required and optional variables
    validateRpcVariables(requiredVariables, optionalVariables);
  }

  public (uint vRef_Id, uint? vRef_Args) GetRpcValueRefsByIdArgs(string id, string args)
  {
    uint? vRef_Id = null;
    uint? vRef_Args = null;
    foreach (var pairNameMember in TerminalMemberVariables)
    {
      if (pairNameMember.Value.MemberName == id)
      {
        vRef_Id = pairNameMember.Value.CorrespondingValueReference;
      }
      else if (pairNameMember.Value.MemberName == args)
      {
        vRef_Args = pairNameMember.Value.CorrespondingValueReference;
      }
    }
    if (!vRef_Id.HasValue)
    {
      throw new TerminalsAndIconsException($"Terminal {Name} does not contain the required terminal member variable " +
                                           $"with memberName {id}.");
    }
    return (vRef_Id.Value, vRef_Args);
  }
}
