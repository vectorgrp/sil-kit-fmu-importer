// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit;

internal sealed class SilKitReturnCodeException : Exception
{
  public SilKitReturnCodeException(
    Helpers.SilKit_ReturnCodes returnCode,
    string? message)
    : base(message)
  {
  }
}
