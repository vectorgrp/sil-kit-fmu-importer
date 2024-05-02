// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter.Exceptions;

public class InvalidCommunicationInterfaceException : Exception
{
  public InvalidCommunicationInterfaceException(string message) : base(message)
  {
  }

  public InvalidCommunicationInterfaceException(string message, Exception? innerException) : base(
    message,
    innerException)
  {
  }
}
