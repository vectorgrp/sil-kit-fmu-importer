// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi.Exceptions;

public class DataConversionException : Exception
{
  public DataConversionException(string message) : base(message)
  {
  }
}
