// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.FmiModel.Internal;

public class TerminalMemberVariable
{
  public string VariableName { get; set; } = null!;
  public string? MemberName { get; set; }
  public string VariableKind { get; set; } = null!;
  public uint? CorrespondingValueReference { get; set; }
}
