// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

/// <summary>
/// Exports the FMI-LS-BUS FMUs built by the pipeline of the sil-kit-fmu-importer-supplements repository
/// (fmi-ls-bus-examples/build_bus_fmus.py) and compares the results with the expected files stored in that repository.
/// These FMUs contain a terminalsAndIcons file: the variables of their network terminals must not be exported.
/// The tests only run if FMU_IMPORTER_SUPPLEMENTS_DIR is set; otherwise they are ignored.
///
/// The expected result of each FMU is found by convention:
///   fmi-ls-bus-examples/_build/X.fmu -> VcdlExporter-Tests/Expected/fmi-ls-bus-examples/X.vcdl
/// Set VCDL_UPDATE_EXPECTED=1 to write the actual results as expected files (review the diff before committing).
/// </summary>
[Category("Supplements")]
public class SupplementsBusFmuTests : ExporterTestBase
{
  private const string SupplementsDirVariable = "FMU_IMPORTER_SUPPLEMENTS_DIR";
  private const string UpdateExpectedVariable = "VCDL_UPDATE_EXPECTED";
  private const string BusFmuDir = "fmi-ls-bus-examples/_build";
  private const string TerminalsAndIconsEntry = "terminalsAndIcons/terminalsAndIcons.xml";

  private static string? SupplementsDir
  {
    get
    {
      return Environment.GetEnvironmentVariable(SupplementsDirVariable);
    }
  }

  public static IEnumerable<TestCaseData> BusFmus()
  {
    if (SupplementsDir == null)
    {
      // NUnit reports a test case source without any case as an error, so a single ignored case is returned
      yield return new TestCaseData("").Ignore($"{SupplementsDirVariable} is not set");
      yield break;
    }

    var path = Path.Combine(SupplementsDir, BusFmuDir);
    if (!Directory.Exists(path))
    {
      yield break;
    }

    foreach (var fmu in Directory.EnumerateFiles(path, "*.fmu").OrderBy(f => f, StringComparer.Ordinal))
    {
      var relativePath = Path.GetRelativePath(SupplementsDir, fmu).Replace('\\', '/');
      yield return new TestCaseData(relativePath).SetArgDisplayNames(relativePath);
    }
  }

  [Test]
  public void Supplements_ContainTheBuiltBusFmus()
  {
    if (SupplementsDir == null)
    {
      Assert.Ignore($"{SupplementsDirVariable} is not set");
    }

    var path = Path.Combine(SupplementsDir!, BusFmuDir);
    Assert.That(
      Directory.Exists(path) && Directory.EnumerateFiles(path, "*.fmu").Any(),
      Is.True,
      $"No FMU was found in '{path}'. The FMI-LS-BUS FMUs must be built before running these tests.");
  }

  [TestCaseSource(nameof(BusFmus))]
  public void BusFmu_TerminalVariablesAreNotExported(string fmu)
  {
    var fmuPath = Path.Combine(SupplementsDir!, fmu);
    using (var archive = ZipFile.OpenRead(fmuPath))
    {
      Assert.That(
        archive.GetEntry(TerminalsAndIconsEntry),
        Is.Not.Null,
        $"'{fmu}' does not contain '{TerminalsAndIconsEntry}', so it cannot test the terminal filtering.");
    }

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);
    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());

    var expectedVcdl = Path.Combine(
      SupplementsDir!,
      "VcdlExporter-Tests",
      "Expected",
      "fmi-ls-bus-examples",
      Path.GetFileNameWithoutExtension(fmu) + ".vcdl");

    if (Environment.GetEnvironmentVariable(UpdateExpectedVariable) == "1")
    {
      Directory.CreateDirectory(Path.GetDirectoryName(expectedVcdl)!);
      File.WriteAllText(expectedVcdl, VcdlAssert.Normalize(File.ReadAllText(OutputVcdl)));
      Assert.Pass($"The expected result was updated in '{expectedVcdl}'.");
    }

    Assert.That(
      File.Exists(expectedVcdl),
      Is.True,
      $"There is no expected result for this FMU. Add '{expectedVcdl}'. " +
      $"The tests write it when {UpdateExpectedVariable}=1 is set.");
    VcdlAssert.MatchesExpectedFile(OutputVcdl, expectedVcdl);
  }
}
