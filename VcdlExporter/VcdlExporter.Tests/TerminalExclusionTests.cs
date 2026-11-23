// SPDX-License-Identifier: MIT
// Copyright (c) Vector Informatik GmbH. All rights reserved.

using VcdlExporter.Tests.Helpers;

namespace VcdlExporter.Tests;

/// <summary>
/// Exports FMUs that contain a terminalsAndIcons file: the variables of network (CAN, Ethernet, LIN) and RPC
/// terminals, of their nested terminals and the clocks of these variables must not be exported.
/// </summary>
[TestFixture]
public class TerminalExclusionTests : ExporterTestBase
{
  private const string TerminalsAndIconsEntry = "terminalsAndIcons/terminalsAndIcons.xml";

  private const string CanMimeType =
    "application/org.fmi-standard.fmi-ls-bus.can; version=&quot;1.0.0&quot;";
  private const string EthernetMimeType =
    "application/org.fmi-standard.fmi-ls-bus.ethernet; version=&quot;1.0.0&quot;";
  private const string LinMimeType =
    "application/org.fmi-standard.fmi-ls-bus.lin; version=&quot;1.0.0&quot;";

  private const string ModelDescriptionXml = $$"""
    <?xml version="1.0" encoding="UTF-8"?>
    <fmiModelDescription fmiVersion="3.0" modelName="Terminals" generationTool="VcdlExporter.Tests" instantiationToken="{00000000-0000-0000-0000-000000000010}">
      <CoSimulation modelIdentifier="Terminals" canHandleVariableCommunicationStepSize="true" hasEventMode="true"/>
      <ModelVariables>
        <Float64 name="time" valueReference="0" causality="independent" variability="continuous"/>
        <Float64 name="RegularInput" valueReference="1" causality="input" variability="discrete" start="0"/>
        <Float64 name="UnknownTerminalValue" valueReference="2" causality="output" variability="discrete"/>

        <Binary name="CanBus_Rx_Data" valueReference="10" causality="input" variability="discrete" mimeType="{{CanMimeType}}" clocks="11"/>
        <Clock name="CanBus_Rx_Clock" valueReference="11" causality="input" intervalVariability="triggered"/>
        <Binary name="CanBus_Tx_Data" valueReference="12" causality="output" variability="discrete" mimeType="{{CanMimeType}}" clocks="13"/>
        <Clock name="CanBus_Tx_Clock" valueReference="13" causality="output" intervalVariability="triggered"/>
        <Float64 name="CanBus_ConfigValue" valueReference="14" causality="input" variability="discrete" start="0"/>

        <Binary name="EthBus_Rx_Data" valueReference="20" causality="input" variability="discrete" mimeType="{{EthernetMimeType}}" clocks="21"/>
        <Clock name="EthBus_Rx_Clock" valueReference="21" causality="input" intervalVariability="triggered"/>

        <Binary name="LinBus_Tx_Data" valueReference="30" causality="output" variability="discrete" mimeType="{{LinMimeType}}" clocks="31"/>
        <Clock name="LinBus_NotAMemberClock" valueReference="31" causality="output" intervalVariability="triggered"/>

        <Int64 name="Rpc_Tx_CallId" valueReference="40" causality="output" variability="discrete"/>
        <Clock name="Rpc_Tx_Call" valueReference="41" causality="output" intervalVariability="triggered"/>
        <Int64 name="Rpc_Rx_ReturnId" valueReference="42" causality="input" variability="discrete" start="0"/>
        <Clock name="Rpc_Rx_Return" valueReference="43" causality="input" intervalVariability="triggered"/>
      </ModelVariables>
      <ModelStructure>
        <Output valueReference="2"/>
        <Output valueReference="12"/>
        <Output valueReference="13"/>
        <Output valueReference="30"/>
        <Output valueReference="31"/>
        <Output valueReference="40"/>
        <Output valueReference="41"/>
      </ModelStructure>
    </fmiModelDescription>
    """;

  // The LIN terminal does not list its clock: it must be excluded because it is the clock of a terminal variable.
  // The CAN terminal has a nested configuration terminal: its variable must be excluded as well.
  private const string TerminalsAndIconsXml = """
    <?xml version="1.0" encoding="UTF-8"?>
    <fmiTerminalsAndIcons fmiVersion="3.0">
      <Terminals>
        <Terminal name="CanBus" matchingRule="org.fmi-ls-bus.transceiver" terminalKind="org.fmi-ls-bus.network-terminal">
          <TerminalMemberVariable variableName="CanBus_Rx_Data" memberName="Rx_Data" variableKind="signal"/>
          <TerminalMemberVariable variableName="CanBus_Rx_Clock" memberName="Rx_Clock" variableKind="signal"/>
          <TerminalMemberVariable variableName="CanBus_Tx_Data" memberName="Tx_Data" variableKind="signal"/>
          <TerminalMemberVariable variableName="CanBus_Tx_Clock" memberName="Tx_Clock" variableKind="signal"/>
          <Terminal name="Configuration" matchingRule="bus" terminalKind="org.fmi-ls-bus.network-terminal.configuration">
            <TerminalMemberVariable variableName="CanBus_ConfigValue" memberName="ConfigValue" variableKind="signal"/>
          </Terminal>
        </Terminal>
        <Terminal name="EthBus" matchingRule="org.fmi-ls-bus.transceiver" terminalKind="org.fmi-ls-bus.network-terminal">
          <TerminalMemberVariable variableName="EthBus_Rx_Data" memberName="Rx_Data" variableKind="signal"/>
          <TerminalMemberVariable variableName="EthBus_Rx_Clock" memberName="Rx_Clock" variableKind="signal"/>
        </Terminal>
        <Terminal name="LinBus" matchingRule="org.fmi-ls-bus.transceiver" terminalKind="org.fmi-ls-bus.network-terminal">
          <TerminalMemberVariable variableName="LinBus_Tx_Data" memberName="Tx_Data" variableKind="signal"/>
        </Terminal>
        <Terminal name="Rpc" matchingRule="plug" terminalKind="vnd.vector.operation-terminal.v1">
          <TerminalMemberVariable variableName="Rpc_Tx_CallId" memberName="Tx_CallId" variableKind="signal"/>
          <TerminalMemberVariable variableName="Rpc_Tx_Call" memberName="Tx_Call" variableKind="signal"/>
          <TerminalMemberVariable variableName="Rpc_Rx_ReturnId" memberName="Rx_ReturnId" variableKind="signal"/>
          <TerminalMemberVariable variableName="Rpc_Rx_Return" memberName="Rx_Return" variableKind="signal"/>
        </Terminal>
        <Terminal name="Unknown" matchingRule="plug" terminalKind="some.unknown.terminal">
          <TerminalMemberVariable variableName="UnknownTerminalValue" memberName="Value" variableKind="signal"/>
        </Terminal>
      </Terminals>
    </fmiTerminalsAndIcons>
    """;

  private static readonly string[] TerminalVariables =
  {
    "CanBus_Rx_Data", "CanBus_Rx_Clock", "CanBus_Tx_Data", "CanBus_Tx_Clock", "CanBus_ConfigValue",
    "EthBus_Rx_Data", "EthBus_Rx_Clock",
    "LinBus_Tx_Data", "LinBus_NotAMemberClock",
    "Rpc_Tx_CallId", "Rpc_Tx_Call", "Rpc_Rx_ReturnId", "Rpc_Rx_Return"
  };

  private string BuildTerminalFmu(string? terminalsAndIconsXml)
  {
    var entries = new Dictionary<string, string> { ["modelDescription.xml"] = ModelDescriptionXml };
    if (terminalsAndIconsXml != null)
    {
      entries[TerminalsAndIconsEntry] = terminalsAndIconsXml;
    }

    return FmuBuilder.BuildFromEntries(Path.Combine(WorkDir, "Terminals.fmu"), entries);
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Export_NetworkAndRpcTerminalVariables_AreNotExported(bool useClockPubSubElements)
  {
    var fmuPath = BuildTerminalFmu(TerminalsAndIconsXml);

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    var vcdl = File.ReadAllText(OutputVcdl);
    Assert.Multiple(() =>
    {
      Assert.That(vcdl, Does.Contain("RegularInput"));
      Assert.That(vcdl, Does.Contain("UnknownTerminalValue"), "variables of unknown terminals are exported");
      foreach (var terminalVariable in TerminalVariables)
      {
        Assert.That(vcdl, Does.Not.Contain(terminalVariable));
      }

      Assert.That(
        result.Output,
        Does.Contain($"{TerminalVariables.Length} variable(s) of network (CAN, Ethernet, LIN) or RPC terminals"));
    });
  }

  [Test]
  public void Export_NoTerminalsAndIconsFile_ExportsAllVariables()
  {
    var fmuPath = BuildTerminalFmu(null);

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl, useClockPubSubElements: true);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.Success), result.ToString());
    var vcdl = File.ReadAllText(OutputVcdl);
    Assert.Multiple(() =>
    {
      Assert.That(vcdl, Does.Contain("RegularInput"));
      Assert.That(vcdl, Does.Contain("UnknownTerminalValue"));
      foreach (var terminalVariable in TerminalVariables)
      {
        Assert.That(vcdl, Does.Contain(terminalVariable));
      }

      Assert.That(result.Output, Does.Not.Contain("will not be exported to the vCDL"));
    });
  }

  [Test]
  public void Export_MalformedTerminalsAndIconsFile_FailsWithFailedToReadTerminalsAndIcons()
  {
    var fmuPath = BuildTerminalFmu("<fmiTerminalsAndIcons fmiVersion=\"3.0\"><Terminals>");

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FailedToReadTerminalsAndIcons), result.ToString());
    Assert.That(result.Output, Does.Contain("terminals and icons").IgnoreCase);
    VcdlAssert.WasNotWritten(OutputVcdl);
  }

  [Test]
  public void Export_InvalidNetworkTerminal_FailsWithFailedToReadTerminalsAndIcons()
  {
    // A CAN terminal with a wrong matching rule is rejected by the terminal validation
    var fmuPath = BuildTerminalFmu(
      TerminalsAndIconsXml.Replace(
        "<Terminal name=\"CanBus\" matchingRule=\"org.fmi-ls-bus.transceiver\"",
        "<Terminal name=\"CanBus\" matchingRule=\"plug\""));

    var result = ExporterRunner.ExportFmu(fmuPath, OutputVcdl);

    Assert.That(result.ExitCode, Is.EqualTo(ExitCodes.FailedToReadTerminalsAndIcons), result.ToString());
    VcdlAssert.WasNotWritten(OutputVcdl);
  }
}
