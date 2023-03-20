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

    var silKitConfigFileOption = new Option<string>(
      name: "--sil-kit-config-file",
      description: "Set the path to the SIL Kit configuration file. Defaults to an empty configuration.");
    silKitConfigFileOption.AddAlias("-c");
    rootCommand.AddOption(silKitConfigFileOption);

    var participantNameOption = new Option<string>(
      name: "--participant-name",
      description: "Set the name of the SIL Kit participant. Defaults to the FMU's model name.");
    participantNameOption.AddAlias("-p");
    rootCommand.AddOption(participantNameOption);

    var ignoreStopTimeOption = new Option<bool>(
      name: "--ignore-stop-time",
      description: "Ignore the FMUs stop time (if it is provided). Stop time is used by default.",
      getDefaultValue: () => false);
    ignoreStopTimeOption.AddAlias("-i");
    rootCommand.AddOption(ignoreStopTimeOption);

    rootCommand.SetHandler((fmuPath, silKitConfigFile, participantName, ignoreStopTime) =>
    {
      if (!File.Exists(fmuPath))
      {
        throw new FileNotFoundException("The provided FMU file path is invalid.");
      }

      if (!File.Exists(silKitConfigFile))
      {
        throw new FileNotFoundException("The provided SIL Kit configuration file path is invalid.");
      }

      var instance = new FmuImporter(fmuPath, silKitConfigFile, participantName, ignoreStopTime);
      instance.RunSimulation();
      instance.Dispose();
    }, fmuPathOption, silKitConfigFileOption, participantNameOption, ignoreStopTimeOption);

    await rootCommand.InvokeAsync(args);
  }
}
