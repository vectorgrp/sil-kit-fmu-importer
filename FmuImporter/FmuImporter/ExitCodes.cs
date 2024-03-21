// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter;

public class ExitCodes
{
  public const int Success = 0;

  public const int ErrorDuringInitialization = 1001;
  public const int ErrorDuringSimulation = 1002;
  public const int ErrorDuringFmuSimulationStepExecution = 1003;
  public const int ErrorDuringUserCallbackExecution = 1004;
  public const int FileNotFound = 1005;

  public const int UnhandledException = -1;
}
