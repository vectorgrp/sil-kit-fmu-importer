// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Exceptions;

public class ParserException : Exception
{
  public ParserException(string? message) : base(message)
  {
  }

  public ParserException(string? message, Exception? innerException) : base(message, innerException)
  {
  }
}
