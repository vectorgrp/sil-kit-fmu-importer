// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.CommandLine;
using System.Globalization;
using FmuImporter.SilKit;
using SilKit.Services.Orchestration;

namespace FmuImporter;

internal class Program
{
  private static async Task Main(string[] args)
  {
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

    var silKitConfigFileOption = new Option<string?>(
      "--sil-kit-config-file",
      "Set the path to the SIL Kit configuration file.");
    silKitConfigFileOption.AddAlias("-s");
    rootCommand.AddOption(silKitConfigFileOption);

    var fmuImporterConfigFileOption = new Option<string?>(
      "--fmu-importer-config-file",
      "Set the path to the FMU Importer configuration file.");
    fmuImporterConfigFileOption.AddAlias("-c");
    rootCommand.AddOption(fmuImporterConfigFileOption);

    var participantNameOption = new Option<string>(
      "--participant-name",
      description: "Set the name of the SIL Kit participant.",
      getDefaultValue: () => "sil-kit-fmu-importer");
    participantNameOption.AddAlias("-p");
    rootCommand.AddOption(participantNameOption);

    // This method is deprecated as of version 1.1.0 (see changelog)
    var useStopTimeOption = new Option<bool>(
      "--use-stop-time",
      () => true);
    useStopTimeOption.AddAlias("-t");
    useStopTimeOption.IsHidden = true;
    rootCommand.AddOption(useStopTimeOption);

    var lifecycleModeOption = new Option<string>(
        "--operation-mode",
        description:
        "Choose the lifecycle mode.",
        getDefaultValue: () => "coordinated")
      .FromAmong(
        "coordinated",
        "autonomous");
    rootCommand.AddOption(lifecycleModeOption);

    var timeSyncModeOption = new Option<string>(
        "--time-sync-mode",
        description:
        "Choose the time synchronization mode.",
        getDefaultValue: () => "synchronized")
      .FromAmong(
        "synchronized",
        "unsynchronized");
    rootCommand.AddOption(timeSyncModeOption);

    var useWallClockAlignmentOption = new Option<string>(
        "--pacing-mode",
        description:
        "Choose the pacing of the simulation.",
        getDefaultValue: () => "as-fast-as-possible")
      .FromAmong(
        "as-fast-as-possible",
        "wall-clock");
    rootCommand.AddOption(useWallClockAlignmentOption);

    rootCommand.SetHandler(
      (
        fmuPath,
        silKitConfigFile,
        fmuImporterConfigFile,
        participantName,
        useStopTime,
        lifecycleMode,
        timeSyncMode,
        pacingMode) =>
      {
        if (!File.Exists(fmuPath))
        {
          throw new FileNotFoundException($"The provided FMU file path ({fmuPath}) is invalid.");
        }

        if (silKitConfigFile != null && !File.Exists(silKitConfigFile))
        {
          throw new FileNotFoundException(
            $"The provided SIL Kit configuration file path ({silKitConfigFile}) is invalid.");
        }

        if (fmuImporterConfigFile != null && !File.Exists(fmuImporterConfigFile))
        {
          throw new FileNotFoundException(
            $"The provided FMU Importer configuration file path ({fmuImporterConfigFile}) is invalid.");
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

        parseSucceeded = Enum.TryParse(
          pacingMode.Replace("-", string.Empty),
          true,
          out PacingModes parsedPacingMode);
        if (!parseSucceeded)
        {
          throw new ArgumentException(
            $"The provided pacing mode '{pacingMode} is invalid. " +
            $"Available options are 'as-fast-as-possible' and 'wall-clock'.");
        }

        var instance = new FmuImporter(
          fmuPath,
          silKitConfigFile,
          fmuImporterConfigFile,
          participantName,
          parsedLifecycleMode,
          parsedTimeSyncMode,
          parsedPacingMode);

        instance.StartSimulation();
        instance.Dispose();
      },
      fmuPathOption,
      silKitConfigFileOption,
      fmuImporterConfigFileOption,
      participantNameOption,
      useStopTimeOption,
      lifecycleModeOption,
      timeSyncModeOption,
      useWallClockAlignmentOption);

    await rootCommand.InvokeAsync(args);
  }
}
