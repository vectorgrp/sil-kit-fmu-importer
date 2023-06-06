// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Exceptions;

public class NativeCallException : Exception
{
  public NativeCallException(string? message) : base(message)
  {
  }
}
