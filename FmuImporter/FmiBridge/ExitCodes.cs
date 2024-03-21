// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi;

public class ExitCodes
{
  public const int Success = 0;

  public const int FailedToLoadLibrary = 2001;
  public const int FailedToReadModelDescription = 2002;
  public const int FmuFailedToTerminate = 2003;
  public const int FmuFailedWithDiscard = 2004;
  public const int FmuFailedWithError = 2005;
  public const int FmuFailedWithFatal = 2006;
}
