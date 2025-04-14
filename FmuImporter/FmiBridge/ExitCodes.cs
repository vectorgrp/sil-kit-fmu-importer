// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi;

public class ExitCodes
{
  private const int Offset = 50;
  public const int Success = 0;

  public const int FailedToLoadLibrary = Offset + 1;
  public const int FailedToReadModelDescription = Offset + 2;
  public const int FmuFailedToTerminate = Offset + 3;
  public const int FmuFailedWithDiscard = Offset + 4;
  public const int FmuFailedWithError = Offset + 5;
  public const int FmuFailedWithFatal = Offset + 6;
  public const int FailedToReadTerminalsAndIcons = Offset + 7;
}
