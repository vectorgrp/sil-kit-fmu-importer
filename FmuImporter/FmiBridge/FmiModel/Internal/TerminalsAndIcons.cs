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
}

