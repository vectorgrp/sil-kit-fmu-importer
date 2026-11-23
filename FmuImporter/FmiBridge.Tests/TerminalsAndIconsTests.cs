// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using System.IO.Compression;
using Fmi;
using Fmi.FmiModel;
using Fmi.FmiModel.Internal;

namespace FmiBridge.Tests;

[TestFixture]
public class TerminalsAndIconsTests
{
  private const string ModelDescriptionXml = """
    <?xml version="1.0" encoding="UTF-8"?>
    <fmiModelDescription fmiVersion="3.0" modelName="TerminalTest" instantiationToken="{00000000-0000-0000-0000-000000000000}">
      <CoSimulation modelIdentifier="TerminalTest" hasEventMode="true" fixedInternalStepSize="0.001"/>
      <ModelVariables>
        <Float64 name="time" valueReference="0" causality="independent" variability="continuous"/>
        <Float64 name="Signal" valueReference="1" causality="input" variability="discrete" start="0"/>
        <Float64 name="UnknownTerminalSignal" valueReference="2" causality="output" variability="discrete"/>
        <Binary name="CanBus.Rx_Data" valueReference="10" causality="input" variability="discrete" mimeType="application/org.fmi-standard.fmi-ls-bus.can; version=&quot;1.0.0&quot;" clocks="11"/>
        <Clock name="CanBus.Rx_Clock" valueReference="11" causality="input" variability="discrete" intervalVariability="triggered"/>
        <Binary name="CanBus.Tx_Data" valueReference="12" causality="output" variability="discrete" mimeType="application/org.fmi-standard.fmi-ls-bus.can; version=&quot;1.0.0&quot;" clocks="13"/>
        <Clock name="CanBus.Tx_Clock" valueReference="13" causality="output" variability="discrete" intervalVariability="triggered"/>
      </ModelVariables>
      <ModelStructure>
        <Output valueReference="2"/>
        <Output valueReference="12"/>
      </ModelStructure>
    </fmiModelDescription>
    """;

  private const string TerminalsAndIconsXml = """
    <?xml version="1.0" encoding="UTF-8"?>
    <fmiTerminalsAndIcons fmiVersion="3.0">
      <Terminals>
        <Terminal name="CanBus" matchingRule="org.fmi-ls-bus.transceiver" terminalKind="org.fmi-ls-bus.network-terminal">
          <TerminalMemberVariable variableName="CanBus.Rx_Data" memberName="Rx_Data" variableKind="signal"/>
          <TerminalMemberVariable variableName="CanBus.Rx_Clock" memberName="Rx_Clock" variableKind="signal"/>
          <TerminalMemberVariable variableName="CanBus.Tx_Data" memberName="Tx_Data" variableKind="signal"/>
          <TerminalMemberVariable variableName="CanBus.Tx_Clock" memberName="Tx_Clock" variableKind="signal"/>
        </Terminal>
        <Terminal name="Unknown" matchingRule="plug" terminalKind="some.unknown.terminal">
          <TerminalMemberVariable variableName="UnknownTerminalSignal" memberName="Value" variableKind="signal"/>
        </Terminal>
      </Terminals>
    </fmiTerminalsAndIcons>
    """;

  private string _tempDirectory = null!;

  [SetUp]
  public void Setup()
  {
    _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(_tempDirectory);
  }

  [TearDown]
  public void TearDown()
  {
    Directory.Delete(_tempDirectory, recursive: true);
  }

  private string CreateFmu(bool withTerminalsAndIcons)
  {
    var fmuPath = Path.Combine(_tempDirectory, "TerminalTest.fmu");
    using var zip = ZipFile.Open(fmuPath, ZipArchiveMode.Create);
    WriteEntry(zip, "modelDescription.xml", ModelDescriptionXml);
    if (withTerminalsAndIcons)
    {
      WriteEntry(zip, "terminalsAndIcons/terminalsAndIcons.xml", TerminalsAndIconsXml);
    }

    return fmuPath;
  }

  private static void WriteEntry(ZipArchive zip, string entryName, string content)
  {
    using var writer = new StreamWriter(zip.CreateEntry(entryName).Open());
    writer.Write(content);
  }

  private static void IgnoreLog(LogSeverity severity, string message)
  {
  }

  [Test]
  public void LoadTerminalsAndIconsFromFmu_NoTerminalsFile_ReturnsNull()
  {
    var fmuPath = CreateFmu(withTerminalsAndIcons: false);
    var modelDescription = ModelLoader.LoadModelFromFmu(fmuPath, IgnoreLog);

    Assert.That(ModelLoader.LoadTerminalsAndIconsFromFmu(fmuPath, modelDescription, IgnoreLog), Is.Null);
  }

  [Test]
  public void LoadTerminalsAndIconsFromFmu_DetectsTerminalKinds()
  {
    var fmuPath = CreateFmu(withTerminalsAndIcons: true);
    var modelDescription = ModelLoader.LoadModelFromFmu(fmuPath, IgnoreLog);

    var terminalsAndIcons = ModelLoader.LoadTerminalsAndIconsFromFmu(fmuPath, modelDescription, IgnoreLog);

    Assert.That(terminalsAndIcons, Is.Not.Null);
    Assert.That(terminalsAndIcons!.Terminals["CanBus"].InternalTerminalKind, Is.EqualTo(InternalTerminalKind.CAN));
    Assert.That(terminalsAndIcons.Terminals["Unknown"].InternalTerminalKind, Is.EqualTo(InternalTerminalKind.UNKNOWN));
  }

  [Test]
  public void GetNetworkAndRpcTerminalValueReferences_ReturnsOnlyNetworkTerminalVariablesAndClocks()
  {
    var fmuPath = CreateFmu(withTerminalsAndIcons: true);
    var modelDescription = ModelLoader.LoadModelFromFmu(fmuPath, IgnoreLog);
    var terminalsAndIcons = ModelLoader.LoadTerminalsAndIconsFromFmu(fmuPath, modelDescription, IgnoreLog);

    var valueRefs = terminalsAndIcons!.GetNetworkAndRpcTerminalValueReferences(modelDescription);

    // CAN data and clock variables are excluded; the regular signal and the unknown terminal's variable are kept
    Assert.That(valueRefs, Is.EquivalentTo(new uint[] { 10, 11, 12, 13 }));
  }
}
