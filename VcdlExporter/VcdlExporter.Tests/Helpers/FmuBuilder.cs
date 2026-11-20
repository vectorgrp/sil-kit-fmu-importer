// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;

namespace VcdlExporter.Tests.Helpers;

/// <summary>
/// Packs a model description fixture into an FMU archive.
/// The FMUs contain no binaries: the exporter only needs the model description.
/// </summary>
public static class FmuBuilder
{
  public static string ModelDescriptionFixture(string relativePath)
  {
    return Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ModelDescriptions", relativePath);
  }

  public static string Build(
    string modelDescriptionPath,
    string targetDirectory,
    IDictionary<string, string>? additionalEntries = null)
  {
    var fmuPath = Path.Combine(targetDirectory, Path.GetFileNameWithoutExtension(modelDescriptionPath) + ".fmu");
    return BuildFromEntries(
      fmuPath,
      new Dictionary<string, string>(additionalEntries ?? new Dictionary<string, string>())
      {
        ["modelDescription.xml"] = File.ReadAllText(modelDescriptionPath)
      });
  }

  public static string BuildFromEntries(string fmuPath, IDictionary<string, string> entries)
  {
    if (File.Exists(fmuPath))
    {
      File.Delete(fmuPath);
    }

    using var archive = ZipFile.Open(fmuPath, ZipArchiveMode.Create);
    foreach (var (entryName, content) in entries)
    {
      var entry = archive.CreateEntry(entryName);
      using var writer = new StreamWriter(entry.Open());
      writer.Write(content);
    }

    return fmuPath;
  }
}
