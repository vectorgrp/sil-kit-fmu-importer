// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

[TestFixture]
public class FmuExportFmi3Tests : ExporterTestBase
{
  // AllTypes: every FMI 3.0 data type, including enumerations (item names with spaces are written with '_')
  // Causalities: only inputs (provided) and outputs (consumed) are exported
  // Arrays: one list<> per dimension, fixed and structural parameter dimensions
  // NoVariables / InputsOnly / OutputsOnly: interface layout without one of the two blocks
  [TestCase("AllTypes")]
  [TestCase("Causalities")]
  [TestCase("Arrays")]
  [TestCase("NoVariables")]
  [TestCase("InputsOnly")]
  [TestCase("OutputsOnly")]
  public void Export_ValidFmu_WritesExpectedVcdl(string fixture)
  {
    var fmuPath = BuildFmu($"Fmi3/{fixture}.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, $"Fmi3/{fixture}.vcdl");
  }

  [Test]
  public void Export_ClocksWithoutClockPubSubElements_OmitsTriggeredClocks()
  {
    var fmuPath = BuildFmu("Fmi3/Clocks.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements: false);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/Clocks.vcdl");
  }

  [Test]
  public void Export_ClocksWithClockPubSubElements_ExportsTriggeredClocksAsBool()
  {
    var fmuPath = BuildFmu("Fmi3/Clocks.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements: true);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/Clocks_ClockPubSubElements.vcdl");
  }

  [Test]
  public void Export_FmuWithoutClocks_ClockPubSubElementsHasNoEffect()
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements: true);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/AllTypes.vcdl");
  }

  [Test]
  public void Export_SameFmuTwice_WritesIdenticalVcdl()
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");
    var secondVcdl = Path.Combine(WorkDir, "second.vcdl");

    ExporterRunner.ExportFmu(fmuPath, OutputVcdl);
    ExporterRunner.ExportFmu(fmuPath, secondVcdl);

    Assert.That(File.ReadAllText(secondVcdl), Is.EqualTo(File.ReadAllText(OutputVcdl)));
  }

  [Test]
  public void Export_OutputFileExists_IsOverwritten()
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");
    File.WriteAllText(OutputVcdl, "previous content that is much longer than the new content would ever be ...");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/AllTypes.vcdl");
  }

#region invalid vCDL identifiers

  [TestCase("InvalidName_Dot", "vehicle.speed")]
  [TestCase("InvalidName_Brackets", "speed[1]")]
  [TestCase("InvalidName_Parentheses", "der(x)")]
  [TestCase("InvalidName_Space", "my speed")]
  [TestCase("InvalidName_LeadingDigit", "1speed")]
  [TestCase("InvalidName_Dash", "speed-kmh")]
  public void Export_VariableNameIsNotAVcdlIdentifier_FailsAndReportsName(string fixture, string invalidName)
  {
    var fmuPath = BuildFmu($"Fmi3/{fixture}.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidIdentifier), result.ToString());
    Assert.That(result.Output, Does.Contain(invalidName), "The invalid name must be reported to the user.");
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_EnumerationTypeNameWithSpace_IsRenamed()
  {
    // Spaces in enumeration type names are replaced by '_', in the definition and in the variables using it
    var fmuPath = BuildFmu("Fmi3/InvalidEnumTypeName.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    var vcdl = File.ReadAllText(OutputVcdl);
    Assert.That(vcdl, Does.Contain("enum My_Option"));
    Assert.That(vcdl, Does.Contain("My_Option Enumeration_input"));
    Assert.That(vcdl, Does.Not.Contain("My Option"));
  }

  [Test]
  public void Export_StructuredNamingConvention_FailsAndReportsAllNames()
  {
    var fmuPath = BuildFmu("Fmi3/StructuredNames.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidIdentifier), result.ToString());
    Assert.That(result.Output, Does.Contain("vehicle.speed"));
    Assert.That(result.Output, Does.Contain("vehicle.acceleration"));
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

#endregion

  [Test]
  public void Export_ModelExchangeOnlyFmu_Fails()
  {
    var fmuPath = BuildFmu("Fmi3/ModelExchangeOnly.xml");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FailedToReadModelDescription), result.ToString());
    Assert.That(result.Output, Does.Contain("does not provide a CoSimulation description"));
    VcdlAssert.WasNotWritten(OutputVcdl);
  }
}
