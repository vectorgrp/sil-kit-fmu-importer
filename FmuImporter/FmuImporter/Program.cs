// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Fmi.FmiModel;
using FmuImporter.SilKit;
using SilKit.Services.Orchestration;

namespace FmuImporter;

internal class Program
{
  private static async Task Main(string[] args)
  {
    AppDomain.CurrentDomain.UnhandledException += PrintUnhandledException;

    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand("FMU Importer for SIL Kit");

    var fmuPathOption = new Option<string>(
      "--fmu-path",
      "Set the path to the FMU file (.fmu). This is mandatory.");
    fmuPathOption.AddAlias("-f");
    fmuPathOption.IsRequired = true;
    rootCommand.AddOption(fmuPathOption);

    // Set the default config file path relative to the executable
    string exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    string defaultConfigPath = Path.Combine(exeDirectory, "Config.silkit.yaml");
    var silKitConfigFileOption = new Option<string?>(
      "--sil-kit-config-file",
      getDefaultValue: () => defaultConfigPath,
      description: "Set the path to the SIL Kit configuration file.");
    silKitConfigFileOption.AddAlias("-s");
    rootCommand.AddOption(silKitConfigFileOption);

    var fmuImporterConfigFileOption = new Option<string?>(
      "--fmu-importer-config-file",
      "Set the path to the FMU Importer configuration file.");
    fmuImporterConfigFileOption.AddAlias("-c");
    fmuImporterConfigFileOption.ArgumentHelpName = "config-file";
    rootCommand.AddOption(fmuImporterConfigFileOption);

    var fmuImporterCommInterfaceFileOption = new Option<string?>(
      "--fmu-importer-communication-interface-file",
      "Set the path to the FMU Importer communication interface file.");
    fmuImporterCommInterfaceFileOption.AddAlias("-i");
    fmuImporterCommInterfaceFileOption.ArgumentHelpName = "communication-interface-file";
    rootCommand.AddOption(fmuImporterCommInterfaceFileOption);

    var participantNameOption = new Option<string>(
      "--participant-name",
      description: "Set the name of the SIL Kit participant.",
      getDefaultValue: () => "sil-kit-fmu-importer");
    participantNameOption.AddAlias("-p");
    rootCommand.AddOption(participantNameOption);

    var lifecycleModeOption = new Option<string>(
        "--operation-mode",
        description:
        "Choose the lifecycle mode.",
        getDefaultValue: () => "unset")
      .FromAmong(
        "coordinated",
        "autonomous");
    lifecycleModeOption.IsHidden = true;
    rootCommand.AddOption(lifecycleModeOption);

    var timeSyncModeOption = new Option<string>(
        "--time-sync-mode",
        description:
        "Choose the time synchronization mode (see documentation).",
        getDefaultValue: () => "synchronized")
      .FromAmong(
        "synchronized",
        "unsynchronized");
    rootCommand.AddOption(timeSyncModeOption);

    var createPersistedFmuOption = new Option<bool>(
      "--persist",
      description: "Unpack the FMU into a persistent directory.",
      getDefaultValue: () => false);
    rootCommand.AddOption(createPersistedFmuOption);

    var usePersistedFmuOption = new Option<bool>(
      "--use-persisted",
      description: "Use a persisted FMU directory (created with \"--persist\" option).",
      getDefaultValue: () => false);
    rootCommand.AddOption(usePersistedFmuOption);

    var useClockPubSubElementsOption = new Option<bool>(
      "--use-clock-pub-sub-elements",
    description: "Handle clocks and clocked variables separately.",
    getDefaultValue: () => false);
    rootCommand.AddOption(useClockPubSubElementsOption);

    rootCommand.SetHandler((context) =>
    {
      var fmuPath = context.ParseResult.GetValueForOption(fmuPathOption);
      var silKitConfigFile = context.ParseResult.GetValueForOption(silKitConfigFileOption)!;
      var fmuImporterConfigFile = context.ParseResult.GetValueForOption(fmuImporterConfigFileOption);
      var fmuImporterCommInterface = context.ParseResult.GetValueForOption(fmuImporterCommInterfaceFileOption);
      var participantName = context.ParseResult.GetValueForOption(participantNameOption)!;
      var lifecycleMode = context.ParseResult.GetValueForOption(lifecycleModeOption);
      var timeSyncMode = context.ParseResult.GetValueForOption(timeSyncModeOption);
      var createPersitentFmu = context.ParseResult.GetValueForOption(createPersistedFmuOption);
      var usePersistedFmu = context.ParseResult.GetValueForOption(usePersistedFmuOption);
      var useClockPubSubElements = context.ParseResult.GetValueForOption(useClockPubSubElementsOption);

      try
      {
          if (!File.Exists(fmuPath))
          {
            throw new FileNotFoundException($"The provided FMU file path ({fmuPath}) is invalid.");
          }

          if(createPersitentFmu)
          {
            // special case early exit : if "--persist" flag is passed, generate the persistent FMU folder and return, no simulation should be started
            ModelLoader.CreatePersistentFmuArtifacts(fmuPath);
            return;
          }

          if (!File.Exists(silKitConfigFile))
          {
            throw new FileNotFoundException(
              $"The provided SIL Kit configuration file path ({silKitConfigFile}) is invalid.");
          }

          if (fmuImporterConfigFile != null && !File.Exists(fmuImporterConfigFile))
          {
            throw new FileNotFoundException(
              $"The provided FMU Importer configuration file path ({fmuImporterConfigFile}) is invalid.");
          }

          if (fmuImporterCommInterface != null && !File.Exists(fmuImporterCommInterface))
          {
            throw new FileNotFoundException(
              $"The provided FMU Importer communication interface file path ({fmuImporterCommInterface}) is invalid.");
          }

          var parseSucceeded = Enum.TryParse(
            lifecycleMode,
            true,
            out LifecycleService.LifecycleConfiguration.Modes parsedLifecycleMode);
          if (!parseSucceeded)
          {
            throw new ArgumentException(
              $"The provided lifecycle mode '{lifecycleMode}' is invalid. " +
              $"Available options are 'autonomous' and 'coordinated'.");
          }

          parseSucceeded = Enum.TryParse(
            timeSyncMode,
            true,
            out TimeSyncModes parsedTimeSyncMode);
          if (!parseSucceeded)
          {
            throw new ArgumentException(
              $"The provided time synchronization mode '{timeSyncMode}' is invalid. " +
              $"Available options are 'synchronized' and 'unsynchronized'.");
          }

          // if lifecycle mode was not set manually...
          //   use coordinated if time-sync-mode = 'synchronized'
          //   use autonomous if time-sync-mode = 'unsynchronized'
          if (lifecycleMode == "unset")
          {
            if (timeSyncMode == "synchronized")
            {
              parsedLifecycleMode = LifecycleService.LifecycleConfiguration.Modes.Coordinated;
            }
            else if (timeSyncMode == "unsynchronized")
            {
              parsedLifecycleMode = LifecycleService.LifecycleConfiguration.Modes.Autonomous;
            }
          }

          var instance = new FmuImporter(
            fmuPath,
            silKitConfigFile,
            fmuImporterConfigFile,
            fmuImporterCommInterface,
            participantName,
            parsedLifecycleMode,
            parsedTimeSyncMode,
            usePersistedFmu,
            useClockPubSubElements
            );

          instance.StartSimulation();
          instance.Dispose();
        }
        catch (Exception e)
        {
          if (Environment.ExitCode == ExitCodes.Success)
          {
            if (e is FileNotFoundException)
            {
              Environment.ExitCode = ExitCodes.FileNotFound;
            }
            else
            {
              Environment.ExitCode = ExitCodes.UnhandledException;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
              $"Encountered exception: {e.Message}.\nMore information was written to the debug console.");
            Debug.WriteLine($"Encountered exception: {e}.");
            Console.ResetColor();
          }
        }
      });
    await rootCommand.InvokeAsync(args);
  }

  private static void PrintUnhandledException(object sender, UnhandledExceptionEventArgs e)
  {
    if (Environment.ExitCode == ExitCodes.Success)
    {
      Environment.ExitCode = ExitCodes.UnhandledException;
    }

    Console.ForegroundColor = ConsoleColor.Red;
    if (e.ExceptionObject is Exception ex)
    {
      Console.WriteLine($"Unhandled exception: {ex.Message}.\nMore information was written to the debug console.");
      Debug.WriteLine($"Unhandled exception: {ex}.");
    }
    else
    {
      Console.WriteLine($"Unhandled non-exception: {e.ExceptionObject}");
    }

    Console.ResetColor();
  }
}
