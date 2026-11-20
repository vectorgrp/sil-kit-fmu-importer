// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

[TestFixture]
public class InvalidFmuTests : ExporterTestBase
{
  [Test]
  public void Export_FmuDoesNotExist_FailsWithFileNotFound()
  {
    var result = ExporterRunner.ExportFmu(Path.Combine(WorkDir, "missing.fmu"), OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FileNotFound), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_OutputDirectoryDoesNotExist_FailsWithFileNotFound()
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");
    var vcdlPath = Path.Combine(WorkDir, "missing", "output.vcdl");

    var result = ExporterRunner.ExportFmu(fmuPath, vcdlPath);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FileNotFound), result.ToString());
    VcdlAssert.WasNotWritten(vcdlPath);
  }

  [Test]
  public void Export_FileIsNotAnArchive_Fails()
  {
    var fmuPath = Path.Combine(WorkDir, "NotAZip.fmu");
    File.WriteAllText(fmuPath, "this is not a zip archive");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FailedToReadModelDescription), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_ArchiveWithoutModelDescription_Fails()
  {
    var fmuPath = FmuBuilder.BuildFromEntries(
      Path.Combine(WorkDir, "NoModelDescription.fmu"),
      new Dictionary<string, string> { ["resources/readme.txt"] = "no model description" });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FailedToReadModelDescription), result.ToString());
    Assert.That(result.Output, Does.Contain("model description").IgnoreCase);
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_UnsupportedFmiVersion_Fails()
  {
    var fmuPath = BuildFmu("UnsupportedFmiVersion.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.ExportFailed), result.ToString());
    Assert.That(result.Output, Does.Contain("version").IgnoreCase);
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_MalformedModelDescription_Fails()
  {
    var fmuPath = BuildFmu("Malformed.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FailedToReadModelDescription), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }
}
