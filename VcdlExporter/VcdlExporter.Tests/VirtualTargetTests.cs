// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

/// <summary>
/// Vector vVIRTUALtarget FMUs ship their own vCDL in the 'resources' folder.
/// The exporter must copy that file unchanged instead of generating one.
/// </summary>
[TestFixture]
public class VirtualTargetTests : ExporterTestBase
{
  // Deliberately different from what the exporter would generate, and with mixed line endings,
  // so that the test detects any regeneration or reformatting
  private const string ShippedVcdl =
    "version 2.0;\r\nimport module \"SilKit\";\n\n// shipped with the vVIRTUALtarget FMU\nnamespace Shipped\n{\n}\n";

  [TestCase("Fmi3")]
  [TestCase("Fmi2")]
  public void Export_VcdlInResources_CopiesShippedVcdlUnchanged(string fmiVersion)
  {
    var fmuPath = BuildFmu(
      $"{fmiVersion}/VirtualTarget.xml",
      new Dictionary<string, string> { ["resources/VirtualTarget.vcdl"] = ShippedVcdl });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    Assert.That(File.ReadAllText(OutputVcdl), Is.EqualTo(ShippedVcdl));
  }

  [Test]
  public void Export_VcdlInResourcesSubfolder_CopiesShippedVcdl()
  {
    var fmuPath = BuildFmu(
      "Fmi3/VirtualTarget.xml",
      new Dictionary<string, string> { ["resources/vcdl/model/VirtualTarget.vcdl"] = ShippedVcdl });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    Assert.That(File.ReadAllText(OutputVcdl), Is.EqualTo(ShippedVcdl));
  }

  [Test]
  public void Export_VcdlInResourcesWithClockPubSubElements_CopiesShippedVcdl()
  {
    var fmuPath = BuildFmu(
      "Fmi3/VirtualTarget.xml",
      new Dictionary<string, string> { ["resources/VirtualTarget.vcdl"] = ShippedVcdl });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements: true);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    Assert.That(File.ReadAllText(OutputVcdl), Is.EqualTo(ShippedVcdl));
    Assert.That(
      result.Output,
      Does.Contain("--use-clock-pub-sub-elements"),
      "The user must be told that the option is ignored.");
  }

  [Test]
  public void Export_OutputFileExists_IsOverwrittenWithShippedVcdl()
  {
    var fmuPath = BuildFmu(
      "Fmi3/VirtualTarget.xml",
      new Dictionary<string, string> { ["resources/VirtualTarget.vcdl"] = ShippedVcdl });
    File.WriteAllText(OutputVcdl, "previous content");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    Assert.That(File.ReadAllText(OutputVcdl), Is.EqualTo(ShippedVcdl));
  }

  [Test]
  public void Export_ResourcesWithoutVcdl_GeneratesVcdl()
  {
    var fmuPath = BuildFmu(
      "Fmi3/VirtualTarget.xml",
      new Dictionary<string, string> { ["resources/readme.txt"] = "no vCDL here" });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/VirtualTarget.vcdl");
  }

  [Test]
  public void Export_NoResourcesFolder_GeneratesVcdl()
  {
    var fmuPath = BuildFmu("Fmi3/VirtualTarget.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/VirtualTarget.vcdl");
  }

  [Test]
  public void Export_VcdlOutsideResources_IsIgnored()
  {
    var fmuPath = BuildFmu(
      "Fmi3/VirtualTarget.xml",
      new Dictionary<string, string>
      {
        ["documentation/VirtualTarget.vcdl"] = ShippedVcdl,
        ["VirtualTarget.vcdl"] = ShippedVcdl
      });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/VirtualTarget.vcdl");
  }

  [Test]
  public void Export_OtherGenerationToolWithVcdlInResources_GeneratesVcdl()
  {
    // Only vVIRTUALtarget FMUs are trusted to ship a matching vCDL
    var fmuPath = BuildFmu(
      "Fmi3/AllTypes.xml",
      new Dictionary<string, string> { ["resources/AllTypes.vcdl"] = ShippedVcdl });

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/AllTypes.vcdl");
  }
}
