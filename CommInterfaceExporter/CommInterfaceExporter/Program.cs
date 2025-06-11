// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.CommandLine;
using System.Globalization;

namespace CommInterfaceExporter;

internal class Program
{
  private static void Main(string[] args)
  {
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    var rootCommand = new RootCommand("Communication Interface Exporter for FMU Importer.");
    
    var fmuPathOption = new Option<string>(
      "--input-path",
      "Set the path to the FMU file (.fmu).");
    fmuPathOption.AddAlias("--fmu-path");
    fmuPathOption.AddAlias("-i");
    fmuPathOption.IsRequired = true;
    rootCommand.AddOption(fmuPathOption);

    var outputPathOption = new Option<string>(
      "--output-path",
      "Target path of the communication interface description file. Must include file ending.");
    outputPathOption.AddAlias("-o");
    outputPathOption.IsRequired = true;
    rootCommand.AddOption(outputPathOption);

    rootCommand.SetHandler(
      (fmuPath, outputPath) =>
      {
        Console.WriteLine("Converting " + fmuPath + " into a communication interface.");

        File.WriteAllText(outputPath, CommInterfaceGenerationWrapper.GenerateFromFile(fmuPath));
        Console.WriteLine("Output written to " + outputPath);
      },
      fmuPathOption,
      outputPathOption);

    rootCommand.Invoke(args);
  }
}
