// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using VcdlExporter.Helpers;

namespace VcdlExporter;

internal class Program
{
  private static async Task<int> Main(string[] args)
  {
    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand(
      "vCDL Exporter for FMI 2.0 / 3.0 FMUs, and FMU Importer communication interface description files.");

    var verboseOption = new Option<bool>(
      "--verbose",
      "Print debug and trace messages, and full error details (including stack traces).");
    verboseOption.AddAlias("-v");
    rootCommand.AddGlobalOption(verboseOption);

    var fmuCommand = new Command("fmu", "Export vCDL based on FMI 2.0 / 3.0 FMUs.");
    rootCommand.AddCommand(fmuCommand);

    var communicationInterfaceCommand = new Command(
      "communicationInterface",
      "Export vCDL based on communication interface description file.");
    rootCommand.AddCommand(communicationInterfaceCommand);

    var vcdlPathOption = new Option<string>(
      "--output-path",
      "Target path of the vCDL. Must include file ending.");
    vcdlPathOption.AddAlias("--vcdl");
    vcdlPathOption.AddAlias("-o");
    vcdlPathOption.IsRequired = true;
    fmuCommand.AddOption(vcdlPathOption);
    communicationInterfaceCommand.AddOption(vcdlPathOption);

    var fmuPathOption = new Option<string>(
      "--input-path",
      "Set the path to the FMU file (.fmu).");
    fmuPathOption.AddAlias("--fmu-path");
    fmuPathOption.AddAlias("-i");
    fmuPathOption.IsRequired = true;
    fmuCommand.AddOption(fmuPathOption);

    var commInterfacePathOption = new Option<string>(
      "--input-path",
      "Set the path to the communication interface description file (.yaml).");
    commInterfacePathOption.AddAlias("--communication-interface-description-path");
    commInterfacePathOption.AddAlias("-i");
    commInterfacePathOption.IsRequired = true;
    communicationInterfaceCommand.AddOption(commInterfacePathOption);

    var interfaceNameOption = new Option<string>(
      "--interface-name",
      "Name of the interface(s) in vCDL. Example: <FMU name>");
    interfaceNameOption.IsRequired = true;
    communicationInterfaceCommand.AddOption(interfaceNameOption);

    var useClockPubSubElementsOption = new Option<bool>(
      "--use-clock-pub-sub-elements",
    description: "Handle clocks and clocked variables separately.",
    getDefaultValue: () => false);
    fmuCommand.AddOption(useClockPubSubElementsOption);

    fmuCommand.SetHandler(
      context =>
      {
        var parseResult = context.ParseResult;
        context.ExitCode = RunExport(
          parseResult.GetValueForOption(verboseOption),
          logger =>
          {
            var fmuExporter = new FmuExporter(
              parseResult.GetValueForOption(fmuPathOption)!,
              parseResult.GetValueForOption(vcdlPathOption)!,
              logger);
            fmuExporter.Export(parseResult.GetValueForOption(useClockPubSubElementsOption));
          });
      });

    communicationInterfaceCommand.SetHandler(
      context =>
      {
        var parseResult = context.ParseResult;
        context.ExitCode = RunExport(
          parseResult.GetValueForOption(verboseOption),
          logger =>
          {
            var commInterfaceExporter = new CommInterfaceExporter(
              parseResult.GetValueForOption(commInterfacePathOption)!,
              parseResult.GetValueForOption(vcdlPathOption)!,
              parseResult.GetValueForOption(interfaceNameOption)!,
              logger);
            commInterfaceExporter.Export();
          });
      });

    return await rootCommand.InvokeAsync(args);
  }

  private static int RunExport(bool verbose, Action<ILogger> export)
  {
    // Disposing the factory flushes the console logger's queue before the process exits
    using var loggerFactory = CreateLoggerFactory(verbose);
    var logger = loggerFactory.CreateLogger("VcdlExporter");

    try
    {
      export(logger);
      return ExitCodes.Success;
    }
    catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
    {
      LogException(logger, e, verbose);
      return ExitCodes.FileNotFound;
    }
    catch (InvalidIdentifierException e)
    {
      LogException(logger, e, verbose);
      return ExitCodes.InvalidIdentifier;
    }
    catch (ModelDescriptionException e)
    {
      LogException(logger, e, verbose);
      return ExitCodes.FailedToReadModelDescription;
    }
    catch (TerminalsAndIconsReadException e)
    {
      LogException(logger, e, verbose);
      return ExitCodes.FailedToReadTerminalsAndIcons;
    }
    catch (Exception e)
    {
      LogException(logger, e, verbose);
      return ExitCodes.ExportFailed;
    }
  }

  private static ILoggerFactory CreateLoggerFactory(bool verbose)
  {
    return LoggerFactory.Create(
      builder => builder
                 .AddConsole(
                   options =>
                   {
                     options.FormatterName = LogFormatter.FormatterName;
                     // Keep stdout free of log messages
                     options.LogToStandardErrorThreshold = LogLevel.Trace;
                   })
                 .AddConsoleFormatter<LogFormatter, ConsoleFormatterOptions>(
                   options => options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff")
                 .SetMinimumLevel(verbose ? LogLevel.Trace : LogLevel.Information));
  }

  private static void LogException(ILogger logger, Exception e, bool verbose)
  {
    if (verbose)
    {
      logger.LogError(e, "Export failed: {Message}", e.Message);
    }
    else
    {
      logger.LogError("Export failed: {Message} Run with --verbose for more details.", e.Message);
    }
  }
}
