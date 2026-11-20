// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

[TestFixture]
public class CommandLineTests : ExporterTestBase
{
  [Test]
  public void NoArguments_FailsWithInvalidArguments()
  {
    var result = ExporterRunner.Run();

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
  }

  [Test]
  public void Help_ListsBothCommands()
  {
    var result = ExporterRunner.Run("--help");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    Assert.That(result.Output, Does.Contain("fmu"));
    Assert.That(result.Output, Does.Contain("communicationInterface"));
  }

  [TestCase("fmu")]
  [TestCase("communicationInterface")]
  public void CommandHelp_Succeeds(string command)
  {
    var result = ExporterRunner.Run(command, "--help");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    Assert.That(result.Output, Does.Contain("--output-path"));
    Assert.That(result.Output, Does.Contain("--input-path"));
  }

  [Test]
  public void UnknownCommand_FailsWithInvalidArguments()
  {
    var result = ExporterRunner.Run("unknown", "-i", "a", "-o", "b");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
  }

  [Test]
  public void Fmu_MissingOutputPath_FailsWithInvalidArguments()
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");

    var result = ExporterRunner.Run("fmu", "-i", fmuPath);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
  }

  [Test]
  public void Fmu_MissingInputPath_FailsWithInvalidArguments()
  {
    var result = ExporterRunner.Run("fmu", "-o", OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Fmu_UnknownOption_FailsWithInvalidArguments()
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");

    var result = ExporterRunner.Run("fmu", "-i", fmuPath, "-o", OutputVcdl, "--unknown-option");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void CommunicationInterface_MissingInterfaceName_FailsWithInvalidArguments()
  {
    var result = ExporterRunner.Run("communicationInterface", "-i", CidFixture("MinValid.yaml"), "-o", OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void CommunicationInterface_ClockPubSubElements_IsNotAnOption()
  {
    var result = ExporterRunner.Run(
      "communicationInterface",
      "-i",
      CidFixture("MinValid.yaml"),
      "-o",
      OutputVcdl,
      "--interface-name",
      "Sample",
      "--use-clock-pub-sub-elements");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidArguments), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [TestCase("-i", "-o")]
  [TestCase("--input-path", "--output-path")]
  [TestCase("--fmu-path", "--vcdl")]
  public void Fmu_OptionAliases_AreAccepted(string inputOption, string outputOption)
  {
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");

    var result = ExporterRunner.Run("fmu", inputOption, fmuPath, outputOption, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/AllTypes.vcdl");
  }

  [TestCase("-i", "-o")]
  [TestCase("--input-path", "--output-path")]
  [TestCase("--communication-interface-description-path", "--vcdl")]
  public void CommunicationInterface_OptionAliases_AreAccepted(string inputOption, string outputOption)
  {
    var result = ExporterRunner.Run(
      "communicationInterface",
      inputOption,
      CidFixture("MinValid.yaml"),
      outputOption,
      OutputVcdl,
      "--interface-name",
      "Sample");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "CIDs/MinValid.vcdl");
  }

  [Test]
  public void Fmu_OptionsInAnyOrder_AreAccepted()
  {
    var fmuPath = BuildFmu("Fmi3/Clocks.xml");

    var result = ExporterRunner.Run("fmu", "--use-clock-pub-sub-elements", "-o", OutputVcdl, "-i", fmuPath);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/Clocks_ClockPubSubElements.vcdl");
  }

  [Test]
  public void Fmu_PathsWithSpaces_AreAccepted()
  {
    var directory = Path.Combine(WorkDir, "folder with spaces");
    Directory.CreateDirectory(directory);
    var fmuPath = FmuBuilder.Build(FmuBuilder.ModelDescriptionFixture("Fmi3/AllTypes.xml"), directory);
    var vcdlPath = Path.Combine(directory, "output file.vcdl");

    var result = ExporterRunner.ExportFmu(fmuPath, vcdlPath);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(vcdlPath, "Fmi3/AllTypes.vcdl");
  }

  [Test]
  public void Fmu_RelativePaths_AreResolvedAgainstWorkingDirectory()
  {
    // The runner starts the exporter in the test directory
    var fmuPath = BuildFmu("Fmi3/AllTypes.xml");
    var testDirectory = TestContext.CurrentContext.TestDirectory;

    var result = ExporterRunner.ExportFmu(
      Path.GetRelativePath(testDirectory, fmuPath),
      Path.GetRelativePath(testDirectory, OutputVcdl));

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "Fmi3/AllTypes.vcdl");
  }
}
