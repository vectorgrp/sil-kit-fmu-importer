// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System;

namespace FmuImporter.Models.Exceptions;

public class InvalidConfigurationException : Exception
{
  public InvalidConfigurationException(string message) : base(message)
  {
  }

  public InvalidConfigurationException(string message, Exception? innerException) : base(message, innerException)
  {
  }
}
