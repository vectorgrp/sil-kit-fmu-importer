// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

[TestFixture]
public class CommInterfaceExportTests : ExporterTestBase
{
  private const string InterfaceName = "Sample";

  // AllScalarTypes: every type alias of the communication interface description
  // Lists: list<> per list level, optional lists and optional items
  // EnumsAndStructs: definitions, nested structs, [Optional] struct members, enum items with spaces
  // NamespaceAndInstance: 'Namespace' and 'Instance' replace the defaults 'FMU' and 'FmuInstance'
  // MinValid: no publishers, no subscribers
  [TestCase("AllScalarTypes")]
  [TestCase("Lists")]
  [TestCase("EnumsAndStructs")]
  [TestCase("NamespaceAndInstance")]
  [TestCase("PublishersOnly")]
  [TestCase("SubscribersOnly")]
  [TestCase("MinValid")]
  public void Export_ValidDescription_WritesExpectedVcdl(string fixture)
  {
    var result = ExporterRunner.ExportCommInterface(CidFixture($"{fixture}.yaml"), OutputVcdl, InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, $"CIDs/{fixture}.vcdl");
  }

  [Test]
  public void Export_InterfaceName_IsUsedForInterfaceAndInstanceType()
  {
    var result = ExporterRunner.ExportCommInterface(CidFixture("PublishersOnly.yaml"), OutputVcdl, "MyModel");

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    var vcdl = VcdlAssert.Normalize(File.ReadAllText(OutputVcdl));
    Assert.That(vcdl, Does.Contain("  interface IMyModel\n"));
    Assert.That(vcdl, Does.Contain("  IMyModel FmuInstance;\n"));
  }

  [Test]
  public void Export_SameNameAsPublisherAndSubscriber_IsOnlyProvided()
  {
    // A feedback loop can only be observed: the signal must be declared once, as provided data
    var result = ExporterRunner.ExportCommInterface(
      CidFixture("SameNamePublisherAndSubscriber.yaml"),
      OutputVcdl,
      InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "CIDs/SameNamePublisherAndSubscriber.vcdl");
  }

  [Test]
  public void Export_OutputFileExists_IsOverwritten()
  {
    File.WriteAllText(OutputVcdl, "previous content that is much longer than the new content would ever be ...");

    var result = ExporterRunner.ExportCommInterface(CidFixture("MinValid.yaml"), OutputVcdl, InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    VcdlAssert.MatchesExpected(OutputVcdl, "CIDs/MinValid.vcdl");
  }

#region failures

  [Test]
  public void Export_DescriptionDoesNotExist_FailsWithFileNotFound()
  {
    var result = ExporterRunner.ExportCommInterface(CidFixture("Missing.yaml"), OutputVcdl, InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FileNotFound), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_OutputDirectoryDoesNotExist_FailsWithFileNotFound()
  {
    var vcdlPath = Path.Combine(WorkDir, "missing", "output.vcdl");

    var result = ExporterRunner.ExportCommInterface(CidFixture("MinValid.yaml"), vcdlPath, InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FileNotFound), result.ToString());
    VcdlAssert.WasNotWritten(vcdlPath);
  }

  [TestCase("Invalid_NoVersion")]
  [TestCase("Invalid_UnknownType")]
  public void Export_InvalidDescription_Fails(string fixture)
  {
    var result = ExporterRunner.ExportCommInterface(CidFixture($"{fixture}.yaml"), OutputVcdl, InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.ExportFailed), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [TestCase("InvalidIdentifier_EnumItem", "3")]
  [TestCase("InvalidIdentifier_Publisher", "vehicle.speed")]
  public void Export_NameIsNotAVcdlIdentifier_Fails(string fixture, string invalidName)
  {
    var result = ExporterRunner.ExportCommInterface(CidFixture($"{fixture}.yaml"), OutputVcdl, InterfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidIdentifier), result.ToString());
    Assert.That(result.Output, Does.Contain($"'{invalidName}'"), "The invalid name must be reported to the user.");
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [TestCase("My.Model")]
  [TestCase("My Model")]
  public void Export_InterfaceNameIsNotAVcdlIdentifier_Fails(string interfaceName)
  {
    var result = ExporterRunner.ExportCommInterface(CidFixture("MinValid.yaml"), OutputVcdl, interfaceName);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.InvalidIdentifier), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

#endregion
}
