// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;

namespace VcdlExporter;

internal class Program
{
  private static async Task Main(string[] args)
  {
    // Set output to be OS language independent
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand(
      "vCDL Exporter for FMI 2.0 / 3.0 FMUs, and FMU Importer communication interface description files.");

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

    fmuCommand.SetHandler(
      (fmuPath, vcdlPath) =>
      {
        try
        {
          var fmuExporter = new FmuExporter(fmuPath, vcdlPath);
          fmuExporter.Export();
        }
        catch (Exception e)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine(
            $"Encountered exception: {e.Message}.\nMore information was written to the debug console.");
          Debug.WriteLine($"Encountered exception: {e}.");
          Console.ResetColor();
        }
      },
      fmuPathOption,
      vcdlPathOption);

    communicationInterfaceCommand.SetHandler(
      (commInterfacePath, vcdlPath, interfaceName) =>
      {
        try
        {
          var fmuExporter = new CommInterfaceExporter(commInterfacePath, vcdlPath, interfaceName);
          fmuExporter.Export();
        }
        catch (Exception e)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine(
            $"Encountered exception: {e.Message}.\nMore information was written to the debug console.");
          Debug.WriteLine($"Encountered exception: {e}.");
          Console.ResetColor();
        }
      },
      commInterfacePathOption,
      vcdlPathOption,
      interfaceNameOption);

    await rootCommand.InvokeAsync(args);
  }
}
