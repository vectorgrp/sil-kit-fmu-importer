// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Binding;

public enum InternalFmuStates
{
  Initial,
  Instantiated,
  ConfigurationMode,
  InitializationMode,
  StepMode,
  Terminated,
  TerminatedWithError,
  Freed
}
