// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

/// <summary>
/// Exports the FMUs built by the pipeline of the sil-kit-fmu-importer-supplements repository and its communication
/// interface descriptions, and compares the results with the expected files stored in that repository.
/// The tests only run if FMU_IMPORTER_SUPPLEMENTS_DIR is set; otherwise they are ignored.
///
/// The expected result of each input is found by convention, so a new input needs no code change:
///   Reference-FMUs/_build/fmus/3.0/X.fmu  -> VcdlExporter-Tests/Expected/Reference-FMUs/3.0/X.vcdl
///   SUT/VanDerPol/X.yaml                  -> VcdlExporter-Tests/Expected/SUT/VanDerPol/X.vcdl
/// If the export must fail, X.exitcode contains the expected exit code instead of X.vcdl.
/// Set VCDL_UPDATE_EXPECTED=1 to write the actual results as expected files (review the diff before committing).
/// </summary>
[Category("Supplements")]
public class SupplementsTests : ExporterTestBase
{
  private const string SupplementsDirVariable = "FMU_IMPORTER_SUPPLEMENTS_DIR";
  private const string UpdateExpectedVariable = "VCDL_UPDATE_EXPECTED";

  private static readonly string[] FmuDirs = { "Reference-FMUs/_build/fmus", "Test-FMUs/_build/fmus" };
  private const string CommInterfaceDir = "SUT";

  // the clock flag only changes the output of FMUs with clocks
  private static readonly string[] FmusWithClocks =
  {
    "Test-FMUs/_build/fmus/3.0/Clocks.fmu",
    "Test-FMUs/_build/fmus/3.0/ListElements.fmu"
  };

  private static string? SupplementsDir
  {
    get
    {
      return Environment.GetEnvironmentVariable(SupplementsDirVariable);
    }
  }

  private static bool UpdateExpected
  {
    get
    {
      return Environment.GetEnvironmentVariable(UpdateExpectedVariable) == "1";
    }
  }

  public static IEnumerable<TestCaseData> Fmus()
  {
    return Discover(FmuDirs, "*.fmu");
  }

  public static IEnumerable<TestCaseData> FmusWithClockPubSubElements()
  {
    return ToTestCases(FmusWithClocks);
  }

  public static IEnumerable<TestCaseData> CommInterfaces()
  {
    return Discover(new[] { CommInterfaceDir }, "*CommInterface*.yaml");
  }

  [Test]
  public void Supplements_ContainTheBuiltFmus()
  {
    IgnoreIfSupplementsAreMissing();

    foreach (var fmuDir in FmuDirs)
    {
      var path = Path.Combine(SupplementsDir!, fmuDir);
      Assert.That(
        Directory.Exists(path) && Directory.EnumerateFiles(path, "*.fmu", SearchOption.AllDirectories).Any(),
        Is.True,
        $"No FMU was found in '{path}'. The FMUs must be built before running these tests.");
    }
  }

  [TestCaseSource(nameof(Fmus))]
  public void Fmu_ExportMatchesExpected(string fmu)
  {
    var result = ExporterRunner.ExportFmu(InputPath(fmu), OutputVcdl);

    CheckResult(result, ExpectedBasePath(fmu));
  }

  [TestCaseSource(nameof(FmusWithClockPubSubElements))]
  public void Fmu_WithClockPubSubElements_ExportMatchesExpected(string fmu)
  {
    var result = ExporterRunner.ExportFmu(InputPath(fmu), OutputVcdl, useClockPubSubElements: true);

    CheckResult(result, ExpectedBasePath(fmu) + "_ClockPubSubElements");
  }

  [TestCaseSource(nameof(CommInterfaces))]
  public void CommInterface_ExportMatchesExpected(string commInterface)
  {
    // the interface is named after the SUT folder, e.g. SUT/StructTest/allOptional/CommInterface.yaml -> StructTest
    var interfaceName = commInterface.Split('/')[1];

    var result = ExporterRunner.ExportCommInterface(InputPath(commInterface), OutputVcdl, interfaceName);

    CheckResult(result, ExpectedBasePath(commInterface));
  }

  private void CheckResult(ExporterResult result, string expectedBasePath)
  {
    var expectedVcdl = expectedBasePath + ".vcdl";
    var expectedExitCodeFile = expectedBasePath + ".exitcode";

    if (UpdateExpected)
    {
      WriteExpected(result, expectedVcdl, expectedExitCodeFile);
      Assert.Pass($"The expected result was updated in '{expectedBasePath}'.");
    }

    if (File.Exists(expectedExitCodeFile))
    {
      var expectedExitCode = int.Parse(File.ReadAllText(expectedExitCodeFile).Trim());
      Assert.That(
        expectedExitCode,
        Is.Not.EqualTo(ExitCodes.Success),
        $"'{expectedExitCodeFile}' must contain the exit code of a failing export; a successful export needs a .vcdl file.");
      Assert.That(result.ExitCode, Is.EqualTo(expectedExitCode), result.ToString());
      VcdlAssert.WasNotWritten(OutputVcdl);
      return;
    }

    Assert.That(
      File.Exists(expectedVcdl),
      Is.True,
      $"There is no expected result for this input. Add '{expectedVcdl}', or '{expectedExitCodeFile}' if the " +
      $"export must fail. The tests write them when {UpdateExpectedVariable}=1 is set.");
    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpectedFile(OutputVcdl, expectedVcdl);
  }

  private void WriteExpected(ExporterResult result, string expectedVcdl, string expectedExitCodeFile)
  {
    // an inconsistent result is a bug, not a new baseline: check it before the existing baseline is replaced
    if (result.ExitCode == ExitCodes.Success)
    {
      Assert.That(
        File.Exists(OutputVcdl),
        Is.True,
        $"The export succeeded without writing a vCDL file; the expected result was not updated.\n{result}");
    }
    else
    {
      Assert.That(
        File.Exists(OutputVcdl),
        Is.False,
        $"The export failed but wrote a vCDL file; the expected result was not updated.\n{result}");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(expectedVcdl)!);
    File.Delete(expectedVcdl);
    File.Delete(expectedExitCodeFile);

    if (result.ExitCode == ExitCodes.Success)
    {
      File.WriteAllText(expectedVcdl, VcdlAssert.Normalize(File.ReadAllText(OutputVcdl)));
    }
    else
    {
      File.WriteAllText(expectedExitCodeFile, result.ExitCode + "\n");
    }
  }

  private static string InputPath(string relativePath)
  {
    return Path.Combine(SupplementsDir!, relativePath);
  }

  private static string ExpectedBasePath(string relativeInputPath)
  {
    var relativePath = relativeInputPath.Replace("_build/fmus/", "");
    return Path.Combine(
      SupplementsDir!,
      "VcdlExporter-Tests",
      "Expected",
      Path.ChangeExtension(relativePath, null));
  }

  private static IEnumerable<TestCaseData> Discover(IEnumerable<string> relativeDirs, string searchPattern)
  {
    if (SupplementsDir == null)
    {
      return ToTestCases(Array.Empty<string>());
    }

    var inputs = relativeDirs
                 .Select(dir => Path.Combine(SupplementsDir, dir))
                 .Where(Directory.Exists)
                 .SelectMany(dir => Directory.EnumerateFiles(dir, searchPattern, SearchOption.AllDirectories))
                 .Select(path => Path.GetRelativePath(SupplementsDir, path).Replace('\\', '/'))
                 .OrderBy(path => path, StringComparer.Ordinal);
    return ToTestCases(inputs);
  }

  private static IEnumerable<TestCaseData> ToTestCases(IEnumerable<string> relativePaths)
  {
    if (SupplementsDir == null)
    {
      // NUnit reports a test case source without any case as an error, so a single ignored case is returned
      yield return new TestCaseData("").Ignore($"{SupplementsDirVariable} is not set");
      yield break;
    }

    foreach (var relativePath in relativePaths)
    {
      yield return new TestCaseData(relativePath).SetArgDisplayNames(relativePath);
    }
  }

  private static void IgnoreIfSupplementsAreMissing()
  {
    if (SupplementsDir == null)
    {
      Assert.Ignore($"{SupplementsDirVariable} is not set");
    }
  }
}
