// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

namespace SilKit.Services.Logger;

public interface ILogger
{
  public void Log(LogLevel level, string message);
  public LogLevel GetLogLevel();
}
