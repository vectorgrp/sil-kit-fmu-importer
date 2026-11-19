// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace VcdlExporter.Helpers;

/// <summary>
/// Prints "[yyyy-MM-dd HH:mm:ss.fff] [level] message", without the logger category and event id.
/// Levels are trace, debug, info, warn and error.
/// </summary>
internal sealed class LogFormatter : ConsoleFormatter
{
  public const string FormatterName = "clean";

  // Width of the longest level name ("trace", "debug", "error"), so that all messages start in the same column
  private const int LabelWidth = 5;

  private readonly IOptionsMonitor<ConsoleFormatterOptions> _options;

  public LogFormatter(IOptionsMonitor<ConsoleFormatterOptions> options) : base(FormatterName)
  {
    _options = options;
  }

  public override void Write<TState>(
    in LogEntry<TState> logEntry,
    IExternalScopeProvider? scopeProvider,
    TextWriter textWriter)
  {
    var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
    if (string.IsNullOrEmpty(message) && logEntry.Exception == null)
    {
      return;
    }

    var label = logEntry.LogLevel switch
    {
      LogLevel.Trace => "trace",
      LogLevel.Debug => "debug",
      LogLevel.Information => "info",
      LogLevel.Warning => "warn",
      // Critical is not used by the exporter; it is reported as an error
      LogLevel.Error or LogLevel.Critical => "error",
      _ => logEntry.LogLevel.ToString().ToLowerInvariant()
    };

    var timestampFormat = _options.CurrentValue.TimestampFormat;
    if (timestampFormat != null)
    {
      var now = _options.CurrentValue.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
      textWriter.Write($"[{now.ToString(timestampFormat)}] ");
    }

    var padding = new string(' ', Math.Max(1, LabelWidth - label.Length + 1));
    textWriter.Write($"[{label}]{padding}");

    textWriter.WriteLine(message);

    if (logEntry.Exception != null)
    {
      textWriter.WriteLine(logEntry.Exception.ToString());
    }
  }
}
