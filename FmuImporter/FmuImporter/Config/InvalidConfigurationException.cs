// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Config;

public class InvalidConfigurationException : Exception
{
  public InvalidConfigurationException(string message) : base(message)
  {
  }
}
