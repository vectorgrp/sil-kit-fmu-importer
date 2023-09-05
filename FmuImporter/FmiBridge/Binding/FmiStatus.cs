// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Binding;

public enum FmiStatus
{
  OK,
  Warning,
  Discard,
  Error,
  Fatal,
  Pending // FMI 2 only
}
