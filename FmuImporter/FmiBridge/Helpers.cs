// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace Fmi;

public class Helpers
{
  public enum LogSeverity
  {
    Error,
    Warning,
    Information,
    Debug,
    Trace
  }

  private static Action<LogSeverity, string>? sLoggerAction;

  public static void SetLoggerCallback(Action<LogSeverity, string> callback)
  {
    sLoggerAction = callback;
  }

  public static void Log(LogSeverity severity, string message)
  {
    if (sLoggerAction != null)
    {
      sLoggerAction.Invoke(severity, message);
    }
    else
    {
      Console.WriteLine($"[{severity}]: {message}");
    }
  }
}
