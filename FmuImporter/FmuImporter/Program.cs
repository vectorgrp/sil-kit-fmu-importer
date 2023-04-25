using System.CommandLine;
using System.Globalization;

namespace FmuImporter;

internal class Program
{
  static async Task Main(string[] args)
  {
    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand("FMU Importer for SIL Kit");

    var fmuPathOption = new Option<string>(
      name: "--fmu-path",
      description: "Set the path to the FMU file (.fmu). This is mandatory.");
    fmuPathOption.AddAlias("-f");
    fmuPathOption.IsRequired = true;
    rootCommand.AddOption(fmuPathOption);

    var silKitConfigFileOption = new Option<string?>(
      name: "--sil-kit-config-file",
      description: "Set the path to the SIL Kit configuration file. Defaults to an empty configuration.",
      getDefaultValue: () => null);
    silKitConfigFileOption.AddAlias("-s");
    rootCommand.AddOption(silKitConfigFileOption);

    var fmuImporterConfigFileOption = new Option<string?>(
      name: "--fmu-importer-config-file",
      description: "Set the path to the FMU Importer configuration file. Defaults to an empty configuration.",
      getDefaultValue: () => null);
    fmuImporterConfigFileOption.AddAlias("-c");
    rootCommand.AddOption(fmuImporterConfigFileOption);

    var participantNameOption = new Option<string>(
      name: "--participant-name",
      description: "Set the name of the SIL Kit participant. Defaults to 'sil-kit-fmu-importer'.",
      getDefaultValue: () => "sil-kit-fmu-importer");
    participantNameOption.AddAlias("-p");
    rootCommand.AddOption(participantNameOption);

    var useStopTimeOption = new Option<bool>(
      name: "--use-stop-time",
      description: "Use the FMUs stop time (if it is provided). Stop time is ignored by default.",
      getDefaultValue: () => false);
    useStopTimeOption.AddAlias("-t");
    rootCommand.AddOption(useStopTimeOption);

    rootCommand.SetHandler((fmuPath, silKitConfigFile, fmuImporterConfigFile, participantName, useStopTime) =>
      {
        if (!File.Exists(fmuPath))
        {
          throw new FileNotFoundException("The provided FMU file path is invalid.");
        }

        if (silKitConfigFile != null && !File.Exists(silKitConfigFile))
        {
          throw new FileNotFoundException("The provided SIL Kit configuration file path is invalid.");
        }

        if (fmuImporterConfigFile != null && !File.Exists(fmuImporterConfigFile))
        {
          throw new FileNotFoundException("The provided FMU Importer configuration file path is invalid.");
        }

        var instance = new FmuImporter(
          fmuPath,
          silKitConfigFile,
          fmuImporterConfigFile,
          participantName,
          useStopTime);

        instance.RunSimulation();
        instance.Dispose();
      },
      fmuPathOption,
      silKitConfigFileOption,
      fmuImporterConfigFileOption,
      participantNameOption,
      useStopTimeOption);

    await rootCommand.InvokeAsync(args);
  }
}
