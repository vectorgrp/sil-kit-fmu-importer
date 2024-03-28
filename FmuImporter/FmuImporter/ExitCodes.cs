// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace FmuImporter;

public class ExitCodes
{
  private const int Offset = 0;
  public const int Success = 0;

  public const int ErrorDuringInitialization = Offset + 1;
  public const int ErrorDuringSimulation = Offset + 2;
  public const int ErrorDuringFmuSimulationStepExecution = Offset + 3;
  public const int ErrorDuringUserCallbackExecution = Offset + 4;
  public const int FileNotFound = Offset + 5;

  public const int UnhandledException = Offset + 49;
}
