// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Binding;

public static class BindingFactory
{
  public static IFmiBindingCommon CreateBinding(
    FmiVersions fmiVersion, string fmuPath, bool usePersistedFmu, Action<LogSeverity, string> logCallback)
  {
    switch (fmiVersion)
    {
      case FmiVersions.Fmi2:
        return new Fmi2Binding(fmuPath, usePersistedFmu, logCallback);
      case FmiVersions.Fmi3:
        return new Fmi3Binding(fmuPath, usePersistedFmu, logCallback);
      default:
        throw new ArgumentOutOfRangeException(nameof(fmiVersion), fmiVersion, null);
    }
  }
}
