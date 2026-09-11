// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using Fmi;

namespace CommInterfaceExporter;

internal class CommInterfaceGenerationWrapper
{
  public static string GenerateFromFile(string fmuPath, bool useClockPubSubElements)
  {
    try
    {
      var FmiVersion = Fmi.FmiModel.ModelLoader.FindFmiVersion(fmuPath);
      var binding = Fmi.Binding.BindingFactory.CreateBinding(FmiVersion, fmuPath, false, LogCallback);
      // Resolve array dimensions so that multi-dimensional arrays are emitted with the correct
      // list nesting (their Dimensions are only populated by InitializeArrayLength).
      foreach (var arrayVar in binding.ModelDescription.ArrayVariables.Values)
      {
        arrayVar.InitializeArrayLength(binding.ModelDescription.Variables);
      }

      return (new Fmi.Supplements.CommInterfaceGenerator(binding, useClockPubSubElements)).CommInterfaceText;
    }
    catch (Exception e)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine(e);
      Console.ResetColor();
      throw;
    }
  }

  private static void LogCallback(LogSeverity arg1, string arg2)
  {
    Console.WriteLine(arg2);
  }


}
