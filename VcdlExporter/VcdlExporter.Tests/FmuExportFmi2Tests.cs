// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

[TestFixture]
public class FmuExportFmi2Tests : ExporterTestBase
{
  // AllTypes: every FMI 2.0 data type, including enumerations (item names with spaces are written with '_')
  // Causalities: only inputs (provided) and outputs (consumed) are exported
  [TestCase("AllTypes")]
  [TestCase("Causalities")]
  [TestCase("NoVariables")]
  public void Export_ValidFmu_WritesExpectedVcdl(string fixture)
  {
    var fmuPath = BuildFmu($"Fmi2/{fixture}.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, $"Fmi2/{fixture}.vcdl");
  }

  [Test]
  public void Export_ClockPubSubElements_HasNoEffect()
  {
    // FMI 2.0 has no clocks
    var fmuPath = BuildFmu("Fmi2/AllTypes.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements: true);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi2/AllTypes.vcdl");
  }

  [TestCase("InvalidName_Dot", "vehicle.speed")]
  [TestCase("InvalidName_Brackets", "speed[1]")]
  public void Export_VariableNameIsNotAVcdlIdentifier_FailsAndReportsName(string fixture, string invalidName)
  {
    var fmuPath = BuildFmu($"Fmi2/{fixture}.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidIdentifier), result.ToString());
    Assert.That(result.Output, Does.Contain(invalidName), "The invalid name must be reported to the user.");
    VcdlAssert.WasNotWritten(OutputVcdl);
  }
}
