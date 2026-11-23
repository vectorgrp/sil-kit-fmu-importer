// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace VcdlExporter;

public class ExitCodes
{
  public const int Success = 0;

  // 1 is returned by System.CommandLine for invalid command line arguments
  public const int InvalidArguments = 1;
  public const int ExportFailed = 2;
  public const int FileNotFound = 3;
  public const int InvalidIdentifier = 4;
  public const int FailedToReadModelDescription = 5;
  public const int FailedToReadTerminalsAndIcons = 6;
}
